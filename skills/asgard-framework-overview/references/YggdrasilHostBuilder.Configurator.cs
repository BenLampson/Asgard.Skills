namespace Asgard.Yggdrasil.AspNetCore;

/// <summary>
/// Yggdrasil 宿主构建器的配置辅助方法集合。
/// </summary>
public partial class YggdrasilHostBuilder
{
    /// <summary>
    /// 配置宿主最终使用的中间件管道。
    /// </summary>
    private async Task ConfigureMiddlewarePipelineAsync(
        WebApplication app,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Configuring middleware pipeline...");

        _ = app.UseAsgardStaticFiles();
        _ = app.UseAsgardRequestTracing();

        // 终结点路由必须先于外部注入的认证/授权中间件执行。
        // 否则 ASP.NET Core 无法把认证中间件与控制器终结点正确关联起来。
        _ = app.UseRouting();

        if (_hostConfig.Cors is { Enabled: true })
        {
            _ = app.UseCors();
        }

        if (_hostConfig.RateLimiting is { Enabled: true })
        {
            _ = app.UseRateLimiter();
        }

        if (_hostConfig.Auth is { Enabled: true })
        {
            _ = app.UseAuthentication();
        }

        _configureMiddleware?.Invoke(app);

        if (_pluginManager != null && _pluginManager.LoadedPlugins.Any())
        {
            _logger.LogDebug("Configuring plugin middleware...");
            await _pluginManager.ConfigureMiddlewareAsync(app, cancellationToken);
            _logger.LogDebug("Plugin middleware configured");
        }
        else
        {
            _logger.LogDebug("No loaded plugins, skipping plugin middleware configuration");
        }

        // 外部认证和声明补充可以替换 principal，完成后再建立授权和仓储共享的租户快照。
        _ = app.UseAsgardTenant();

        if (_hostConfig.RateLimiting is { Enabled: true, User: { Enabled: true } })
        {
            // 用户分区必须在宿主认证及外部身份中间件之后执行，
            // 确保 HttpContext.User 已包含可用于分区的稳定主体声明。
            _ = app.UseMiddleware<YggdrasilUserRateLimitingMiddleware>();
        }

        // 授权中间件需要始终接入管线。
        // 即便宿主关闭了内置 JWT 认证，外部模块或插件仍可能注册自定义认证方案、
        // 写入身份上下文或补充声明信息，因此必须在这些扩展中间件之后再统一执行授权。
        _ = app.UseAuthorization();

        MapOpenApiDocumentation(app);
        MapHealthChecks(app);
        MapTsGenerationEndpoint(app);
        _ = app.MapControllers();

        _logger.LogDebug("Middleware pipeline configured");
    }

    /// <summary>
    /// 执行第一阶段：构建配置图。
    /// </summary>
    private WebApplicationBuilder ExecutePhase1ConfigurationLoading()
    {
        // 先确定要读取哪个环境文件。显式 YAML 可以选定环境，省略时使用部署环境。
        var arguments = Environment.GetCommandLineArgs().Skip(1);
        var selection = CreateProcessConfiguration(arguments).AddYamlFile(_configFilePath).Build();
        var profilePath = GetEnvironmentConfigPath(selection.GetScalar("host.application.environment")!);
        AsgardConfigurationRoot LoadConfiguration() => CreateProcessConfiguration(arguments)
            .AddYamlFile(profilePath, optional: true)
            .AddYamlFile(_configFilePath)
            .Build();

        _asgardConfiguration = LoadConfiguration();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = _asgardConfiguration.GetScalar("host.application.environment")
        });
        if (_beforeConfigurationLoad is not null)
        {
            _beforeConfigurationLoad(builder.Host);
            // 钩子可以调整配置来源。重新读取后让观察回调、强类型配置和运行环境使用同一值。
            _asgardConfiguration = LoadConfiguration();
            builder.Environment.EnvironmentName = _asgardConfiguration.GetScalar("host.application.environment")!;
        }

        var snapshot = _asgardConfiguration.ToConfigurationDictionary();
        builder.Configuration.Sources.Add(new AsgardHostConfigurationProvider(_asgardConfiguration, snapshot));

        _logConfig = _asgardConfiguration.Load<LogConfig>();
        _logConfig.Validate();
        _bootstrapLoggerFactory = CreateStartupLoggerFactory(_logConfig);
        _runtimeLifetime.BootstrapLoggerFactory = _bootstrapLoggerFactory;
        _logger = _bootstrapLoggerFactory.CreateLogger("YggdrasilHostBuilder");
        _logger.LogInformation("Asgard Yggdrasil host starting...");
        _afterConfigurationLoad?.Invoke(snapshot);
        _logger.LogInformation("[Phase 1/5] Configuration graph built from Asgard sources ({Path})", _configFilePath);
        return builder;
    }

    /// <summary>进程配置补齐 YAML 中缺少的键，环境变量优先于命令行参数。</summary>
    internal static AsgardConfigurationBuilder CreateProcessConfiguration(IEnumerable<string> arguments) => new AsgardConfigurationBuilder()
        .AddValue("host.application.environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Development)
        .AddCommandLine(arguments)
        .AddEnvironmentVariables();

    private string GetEnvironmentConfigPath(string environmentName)
    {
        ApplicationOptions.ValidateEnvironmentName(environmentName);
        var directory = Path.GetDirectoryName(_configFilePath) ?? "config";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_configFilePath);
        var extension = Path.GetExtension(_configFilePath);
        return Path.Combine(directory, $"{fileNameWithoutExt}.{environmentName}{extension}");
    }

    /// <summary>
    /// 从最终生效的日志配置创建宿主或插件装配阶段使用的日志工厂。
    /// </summary>
    private static ILoggerFactory CreateStartupLoggerFactory(LogConfig logConfig)
    {
        var serilogConfig = SerilogConfigurator.FromConfig(logConfig, startRetentionCleanup: false);
        var serilogLogger = serilogConfig.CreateLogger();
        return new LoggerFactory().AddSerilog(serilogLogger, dispose: true);
    }

    private void MapHealthChecks(WebApplication app)
    {
        if (_hostConfig.HealthCheck is not { Enabled: true } healthCheckOptions)
        {
            return;
        }

        var mappedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        MapHealthCheckEndpoint(
            app,
            mappedPaths,
            healthCheckOptions.Path,
            static _ => true);
        MapHealthCheckEndpoint(
            app,
            mappedPaths,
            healthCheckOptions.ReadyPath,
            registration => registration.Tags.Contains("ready", StringComparer.OrdinalIgnoreCase));
        MapHealthCheckEndpoint(
            app,
            mappedPaths,
            healthCheckOptions.LivePath,
            registration => registration.Tags.Contains("live", StringComparer.OrdinalIgnoreCase));
    }

    private static void MapHealthCheckEndpoint(
        WebApplication app,
        ISet<string> mappedPaths,
        string path,
        Func<HealthCheckRegistration, bool> predicate)
    {
        if (!mappedPaths.Add(path))
        {
            return;
        }

        _ = app.MapHealthChecks(path, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = predicate
        });
    }
}
