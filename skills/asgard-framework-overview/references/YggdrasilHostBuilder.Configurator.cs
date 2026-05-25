namespace Asgard.Yggdrasil.AspNetCore;

/// <summary>
/// Yggdrasil 宿主构建器的配置辅助方法集合。
/// </summary>
public partial class YggdrasilHostBuilder
{
    /// <summary>
    /// 配置宿主最终使用的中间件管道。
    /// </summary>
    private void ConfigureMiddlewarePipeline(WebApplication app)
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

        _ = app.UseAsgardTenant();

        _configureMiddleware?.Invoke(app);

        if (_pluginManager != null && _pluginManager.LoadedPlugins.Any())
        {
            _logger.LogDebug("Configuring plugin middleware...");
            _pluginManager.ConfigureMiddlewareAsync(app).GetAwaiter().GetResult();
            _logger.LogDebug("Plugin middleware configured");
        }
        else
        {
            _logger.LogDebug("No loaded plugins, skipping plugin middleware configuration");
        }

        // 授权中间件需要始终接入管线。
        // 即便宿主关闭了内置 JWT 认证，外部模块或插件仍可能注册自定义认证方案、
        // 写入身份上下文或补充声明信息，因此必须在这些扩展中间件之后再统一执行授权。
        _ = app.UseAuthorization();

        if (_hostConfig.Swagger is { Enabled: true })
        {
            _ = app.UseSwagger();
            _ = app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    $"/{_hostConfig.Swagger.RoutePrefix}/{_hostConfig.Swagger.Version}/swagger.json",
                    _hostConfig.Swagger.Title);
                options.RoutePrefix = _hostConfig.Swagger.RoutePrefix;
            });

            _logger.LogDebug("Swagger middleware enabled with route prefix {RoutePrefix}", _hostConfig.Swagger.RoutePrefix);
        }

        MapHealthChecks(app);
        MapTsGenerationEndpoint(app);
        _ = app.MapControllers();

        _logger.LogDebug("Middleware pipeline configured");
    }

    /// <summary>
    /// 执行第一阶段：构建配置图。
    /// </summary>
    private void ExecutePhase1ConfigurationLoading(WebApplicationBuilder builder)
    {
        _beforeConfigurationLoad?.Invoke(builder.Host);

        var commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
        _asgardConfiguration = new AsgardConfigurationBuilder()
            .AddYamlFile(_configFilePath)
            .AddYamlFile(GetEnvironmentConfigPath(builder.Environment.EnvironmentName), optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(commandLineArgs)
            .Build();

        _ = builder.Configuration.AddInMemoryCollection(_asgardConfiguration.ToConfigurationDictionary());

        _afterConfigurationLoad?.Invoke(builder.Host, builder.Configuration);
        _logger.LogInformation("[Phase 1/5] Configuration graph built from Asgard sources ({Path})", _configFilePath);
    }

    private string GetEnvironmentConfigPath(string environmentName)
    {
        var directory = Path.GetDirectoryName(_configFilePath) ?? "config";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_configFilePath);
        var extension = Path.GetExtension(_configFilePath);
        return Path.Combine(directory, $"{fileNameWithoutExt}.{environmentName}{extension}");
    }

    /// <summary>
    /// 创建宿主启动阶段使用的引导日志工厂。
    /// </summary>
    private ILoggerFactory CreateBootstrapLoggerFactory()
    {
        var logConfig = YamlConfigLoader.LoadFromFile<LogConfig>(_configFilePath);
        logConfig.Validate();

        var serilogConfig = SerilogConfigurator.FromConfig(logConfig);
        var serilogLogger = serilogConfig.CreateLogger();
        return new LoggerFactory().AddSerilog(serilogLogger);
    }

    /// <summary>
    /// 创建插件服务发现阶段使用的日志记录器。
    /// </summary>
    private Microsoft.Extensions.Logging.ILogger CreatePluginServiceConfiguratorLogger()
    {
        var logConfig = _logConfig ?? YamlConfigLoader.LoadFromFile<LogConfig>(_configFilePath);
        logConfig.Validate();

        var serilogConfig = SerilogConfigurator.FromConfig(logConfig);
        var serilogLogger = serilogConfig.CreateLogger();
        var loggerFactory = new LoggerFactory().AddSerilog(serilogLogger);
        return loggerFactory.CreateLogger("PluginServiceConfigurator");
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
