namespace Asgard.Yggdrasil.AspNetCore;

public partial class YggdrasilHostBuilder
{
    private readonly string _configFilePath;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly ILoggerFactory _bootstrapLoggerFactory;
    private HostConfig _hostConfig;

    private Action<IHostBuilder>? _beforeConfigurationLoad;
    private Action<IHostBuilder, IConfiguration>? _afterConfigurationLoad;
    private Action<IServiceCollection>? _beforeServiceRegistration;
    private Action<IServiceCollection>? _afterServiceRegistration;
    private Action<IHost>? _afterHostBuild;
    private Action<IApplicationBuilder>? _configureMiddleware;
    private readonly HashSet<Type> _builtInPluginTypes = [];
    private AsgardConfigurationRoot? _asgardConfiguration;
    private PluginConfig? _pluginConfig;
    private IReadOnlyList<PluginDescriptor> _pluginDescriptors = Array.Empty<PluginDescriptor>();
    private PluginManager? _pluginManager;

    private CacheManager? _cacheManager;
    private MQManager? _mqManager;
    private JobManager? _jobManager;

    private LogConfig? _logConfig;
    private CacheConfig? _cacheConfig;
    private MQConfig? _mqConfig;
    private JobConfig? _jobConfig;
    private DatabaseConfig? _databaseConfig;

    internal YggdrasilHostBuilder(string configFilePath)
    {
        _configFilePath = configFilePath;
        _hostConfig = YamlConfigLoader.LoadFromFile<HostConfig>(_configFilePath);
        _hostConfig.Validate();
        _bootstrapLoggerFactory = CreateBootstrapLoggerFactory();
        _logger = _bootstrapLoggerFactory.CreateLogger("YggdrasilHostBuilder");
    }

    /// <summary>
    /// 注册在框架服务加入容器之前执行的回调。
    /// </summary>
    /// <param name="action">要执行的回调。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder BeforeServiceRegistration(Action<IServiceCollection> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _beforeServiceRegistration = action;
        return this;
    }

    /// <summary>
    /// 注册在 Asgard 配置源装载之前执行的回调。
    /// </summary>
    /// <param name="action">要执行的回调。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder BeforeConfigurationLoad(Action<IHostBuilder> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _beforeConfigurationLoad = action;
        return this;
    }

    /// <summary>
    /// 注册在合并配置可用之后执行的回调。
    /// </summary>
    /// <param name="action">要执行的回调。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder AfterConfigurationLoad(Action<IHostBuilder, IConfiguration> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _afterConfigurationLoad = action;
        return this;
    }

    /// <summary>
    /// 注册在框架服务注册完成之后执行的回调。
    /// </summary>
    /// <param name="action">要执行的回调。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder AfterServiceRegistration(Action<IServiceCollection> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _afterServiceRegistration = action;
        return this;
    }

    /// <summary>
    /// 注册在宿主构建完成之后执行的回调。
    /// </summary>
    /// <param name="action">要执行的回调。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder AfterHostBuild(Action<IHost> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _afterHostBuild = action;
        return this;
    }

    /// <summary>
    /// 为已构建应用注册中间件配置回调。
    /// </summary>
    /// <param name="action">中间件配置回调。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder ConfigureMiddleware(Action<IApplicationBuilder> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _configureMiddleware = action;
        return this;
    }

    /// <summary>
    /// 注册当前应用中的内建插件类型，使其无需文件系统扫描也能参与插件生命周期。
    /// </summary>
    /// <typeparam name="TPlugin">插件类型。</typeparam>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder UseBuiltInPlugin<TPlugin>() where TPlugin : class, IPlugin
    {
        return UseBuiltInPlugin(typeof(TPlugin));
    }

    /// <summary>
    /// 注册当前应用中的内建插件类型，使其无需文件系统扫描也能参与插件生命周期。
    /// </summary>
    /// <param name="pluginType">插件类型。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder UseBuiltInPlugin(Type pluginType)
    {
        ArgumentNullException.ThrowIfNull(pluginType);

        if (!typeof(IPlugin).IsAssignableFrom(pluginType) || pluginType.IsAbstract || pluginType.IsInterface)
        {
            throw new ArgumentException(
                $"Type {pluginType.FullName} must be a non-abstract implementation of {nameof(IPlugin)}.",
                nameof(pluginType));
        }

        _ = _builtInPluginTypes.Add(pluginType);
        return this;
    }

    /// <summary>
    /// 将指定程序集内发现的所有插件实现注册为内建插件。
    /// </summary>
    /// <param name="assembly">要扫描的程序集。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder UseBuiltInPluginsFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var pluginTypes = assembly.GetTypes()
            .Where(type => typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            .ToList();

        if (pluginTypes.Count == 0)
        {
            throw new InvalidOperationException($"No {nameof(IPlugin)} implementations were found in {assembly.FullName}.");
        }

        foreach (var pluginType in pluginTypes)
        {
            _ = _builtInPluginTypes.Add(pluginType);
        }

        return this;
    }

    /// <summary>
    /// 将入口程序集内发现的所有插件实现注册为内建插件。
    /// </summary>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder UseEntryAssemblyPlugins()
    {
        var entryAssembly = Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("Entry assembly is not available for built-in plugin discovery.");

        return UseBuiltInPluginsFromAssembly(entryAssembly);
    }

    /// <summary>
    /// 构建当前已完成配置的 Web 应用。
    /// </summary>
    /// <returns>构建完成的 Web 应用。</returns>
    public WebApplication Build()
    {
        _logger.LogInformation("Asgard Yggdrasil host starting...");

        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? _hostConfig.Application.Environment;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });

        ExecutePhase1ConfigurationLoading(builder);
        LoadModuleConfigs();
        ApplyHostRuntimeConfiguration(builder);
        InitializeInfrastructureManagers();
        ExecutePhase2ServiceRegistration(builder.Host);

        var app = builder.Build();

        _jobManager?.SetServiceProvider(app.Services);

        ResolvePluginManager(app.Services);
        InitializePluginManager();

        ConfigureMiddlewarePipeline(app);
        _afterHostBuild?.Invoke(app);

        return app;
    }

    private void LoadModuleConfigs()
    {
        ArgumentNullException.ThrowIfNull(_asgardConfiguration);

        _hostConfig = _asgardConfiguration.Load<HostConfig>();
        _hostConfig.Validate();

        _logConfig = _asgardConfiguration.Load<LogConfig>();
        _logConfig.Validate();

        _cacheConfig = _asgardConfiguration.Load<CacheConfig>();
        _cacheConfig.Validate();

        _databaseConfig = _asgardConfiguration.Load<DatabaseConfig>();
        _databaseConfig.Validate();

        _mqConfig = _asgardConfiguration.Load<MQConfig>();
        _mqConfig.Validate();

        _jobConfig = _asgardConfiguration.Load<JobConfig>();
        _jobConfig.Validate();

        _pluginConfig = _asgardConfiguration.Load<PluginConfig>();
        _pluginConfig.Validate();

        if (_builtInPluginTypes.Count > 0 && !_pluginConfig.Enabled)
        {
            _pluginConfig.Enabled = true;
            _logger.LogDebug("检测到已注册内建插件，已自动启用插件系统。");
        }

        _logger.LogInformation("[Phase 2/5] Module configuration loaded");
    }

    private void InitializeInfrastructureManagers()
    {
        var cacheStatus = "SKIP";
        var mqStatus = "SKIP";
        var jobStatus = "SKIP";

        if (_cacheConfig!.Enabled)
        {
            _cacheManager = new CacheManager(_cacheConfig, _bootstrapLoggerFactory.CreateLogger<CacheManager>());
            _cacheManager.InitializeAsync().GetAwaiter().GetResult();
            cacheStatus = "OK";
        }

        if (_mqConfig!.Enabled)
        {
            _mqManager = new MQManager(_mqConfig, _bootstrapLoggerFactory.CreateLogger<MQManager>());
            _mqManager.InitializeAsync().GetAwaiter().GetResult();
            mqStatus = "OK";
        }

        if (_jobConfig!.Enabled)
        {
            _jobManager = new JobManager(_jobConfig, _bootstrapLoggerFactory, _bootstrapLoggerFactory.CreateLogger<JobManager>());
            _jobManager.InitializeAsync().GetAwaiter().GetResult();
            jobStatus = "OK";
        }

        _logger.LogInformation(
            "[Phase 3/5] Infrastructure initialized (cache: {Cache}, mq: {MQ}, job: {Job})",
            cacheStatus,
            mqStatus,
            jobStatus);
    }
}
