namespace Asgard.Yggdrasil.AspNetCore;

public partial class YggdrasilHostBuilder
{
    private readonly string _configFilePath;
    private readonly AsgardRuntimeLifetime _runtimeLifetime = new();
    private int _buildStarted;
    // 构建时从同一份有效配置初始化日志和宿主选项。
    private Microsoft.Extensions.Logging.ILogger _logger = null!;
    private ILoggerFactory _bootstrapLoggerFactory = null!;
    private HostConfig _hostConfig = null!;

    private Action<IHostBuilder>? _beforeConfigurationLoad;
    private Action<IReadOnlyDictionary<string, string?>>? _afterConfigurationLoad;
    private Action<IServiceCollection>? _beforeServiceRegistration;
    private Action<IServiceCollection>? _afterServiceRegistration;
    private Action<IHost>? _afterHostBuild;
    private Action<IApplicationBuilder>? _configureMiddleware;
    private readonly HashSet<Type> _builtInPluginTypes = [];
    private AsgardConfigurationRoot? _asgardConfiguration;
    private PluginConfig? _pluginConfig;
    private IReadOnlyList<PluginDescriptor> _pluginDescriptors = Array.Empty<PluginDescriptor>();
    private PluginManager? _pluginManager;
    private bool _pluginServicesPrepared;

    private CacheManager? _cacheManager;
    private MQManager? _mqManager;
    private JobManager? _jobManager;
    private IJobScheduler? _defaultScheduler;
    private IMessageQueue? _defaultQueue;
    private Func<IJobScheduler>? _managedScheduler;
    private Func<IMessageQueue>? _managedQueue;
    private ServiceDescriptor? _defaultTraceRegistration;
    private bool _managerProvidesConnection;

    private LogConfig? _logConfig;
    private TraceOptions? _traceConfig;
    private CacheConfig? _cacheConfig;
    private DistributedLockOptions? _distributedLockOptions;
    private MQConfig? _mqConfig;
    private JobConfig? _jobConfig;
    private DatabaseConfig? _databaseConfig;

    internal YggdrasilHostBuilder(string configFilePath)
    {
        if (!File.Exists(configFilePath)) throw new FileNotFoundException($"配置文件未找到：{configFilePath}", configFilePath);
        _configFilePath = configFilePath;
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
    /// 注册在 Asgard 配置合并完成后执行的观察回调。
    /// </summary>
    /// <param name="action">接收不可变配置快照的回调，键使用冒号分隔。</param>
    /// <returns>当前构建器。</returns>
    public YggdrasilHostBuilder AfterConfigurationLoad(Action<IReadOnlyDictionary<string, string?>> action)
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
        // 保留原有同步 API；真正的启动流程由 BuildAsync 承担，避免调用方继续堆叠同步阻塞。
        return BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 异步构建当前已完成配置的 Web 应用。
    /// </summary>
    /// <param name="cancellationToken">用于取消基础设施初始化和插件生命周期的令牌。</param>
    /// <returns>构建完成的 Web 应用。</returns>
    public async Task<WebApplication> BuildAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _buildStarted, 1) != 0)
            return await CreateIndependentBuilder().BuildAsync(cancellationToken);

        WebApplication? app = null;
        try
        {
            var builder = ExecutePhase1ConfigurationLoading();
            LoadModuleConfigs();
            ApplyHostRuntimeConfiguration(builder);
            await PreparePluginServicesAsync(builder.Services, cancellationToken);
            _runtimeLifetime.PreparedPlugins = _pluginDescriptors;
            ExecutePhase2ServiceRegistration(builder.Host);
            await SelectInfrastructureAsync(builder.Services, cancellationToken);
            FeatureRegistrations.Register(builder.Services, final: true);
            app = builder.Build();
            _runtimeLifetime.Services = app.Services;
            await InitializeInfrastructureAsync(app.Services, cancellationToken);
            ResolvePluginManager(app.Services);
            _runtimeLifetime.Plugins = _pluginManager;
            if (_pluginManager is not null)
                _runtimeLifetime.PreparedPlugins = Array.Empty<PluginDescriptor>();
            _pluginDescriptors = Array.Empty<PluginDescriptor>();
            // 主动解析所有者，确保未调用 StartAsync 的宿主也能释放资源。
            // 在插件管理器之后解析，使所有者先于容器的兜底释放执行清理。
            _ = app.Services.GetRequiredService<AsgardRuntimeLifetime>();
            await InitializePluginManagerAsync(cancellationToken);
            await ConfigureMiddlewarePipelineAsync(app, cancellationToken);
            _afterHostBuild?.Invoke(app);
            return app;
        }
        catch (Exception startupError)
        {
            var failures = new List<Exception> { startupError };
            try { await _runtimeLifetime.DisposeAsync(); }
            catch (Exception cleanupError) { failures.Add(cleanupError); }
            if (app is not null)
            {
                try { await app.DisposeAsync(); }
                catch (Exception cleanupError)
                {
                    if (!failures.Contains(cleanupError)) failures.Add(cleanupError);
                }
            }
            _pluginDescriptors = Array.Empty<PluginDescriptor>();
            if (failures.Count > 1) throw new AggregateException("宿主启动及清理失败。", failures);
            throw;
        }
    }

    /// <summary>构建并运行宿主，启动失败时在服务容器释放前回滚。</summary>
    /// <param name="cancellationToken">用于取消构建和运行的令牌。</param>
    /// <returns>宿主完全停止并释放时完成。</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var app = await BuildAsync(cancellationToken);
        await YggdrasilHost.RunAsync(app, cancellationToken);
    }

    /// <summary>保留重复 Build 的公开行为，但每个宿主使用独立的资源所有者和插件实例。</summary>
    private YggdrasilHostBuilder CreateIndependentBuilder()
    {
        var builder = new YggdrasilHostBuilder(_configFilePath)
        {
            _beforeConfigurationLoad = _beforeConfigurationLoad,
            _afterConfigurationLoad = _afterConfigurationLoad,
            _beforeServiceRegistration = _beforeServiceRegistration,
            _afterServiceRegistration = _afterServiceRegistration,
            _afterHostBuild = _afterHostBuild,
            _configureMiddleware = _configureMiddleware
        };
        builder._builtInPluginTypes.UnionWith(_builtInPluginTypes);
        return builder;
    }

    private void LoadModuleConfigs()
    {
        ArgumentNullException.ThrowIfNull(_asgardConfiguration);

        _hostConfig = _asgardConfiguration.Load<HostConfig>();
        _hostConfig.Validate();

        _traceConfig = _asgardConfiguration.Load<TraceOptions>();
        _traceConfig.Validate();

        _cacheConfig = _asgardConfiguration.Load<CacheConfig>();
        _cacheConfig.Validate();

        _distributedLockOptions = _asgardConfiguration.Load<DistributedLockOptions>();
        _distributedLockOptions.Validate();

        _databaseConfig = _asgardConfiguration.Load<DatabaseConfig>();
        _databaseConfig.Validate();

        _mqConfig = _asgardConfiguration.Load<MQConfig>();
        _mqConfig.Validate();

        _jobConfig = _asgardConfiguration.Load<JobConfig>();
        _jobConfig.Validate();

        _pluginConfig = _asgardConfiguration.Load<PluginConfig>();
        _pluginConfig.Validate();

        _logger.LogInformation("[Phase 2/5] Module configuration loaded");
    }
}
