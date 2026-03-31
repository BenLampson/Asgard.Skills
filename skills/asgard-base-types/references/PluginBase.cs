[assembly: InternalsVisibleTo("Asgard.Core.Tests")]

namespace Asgard.Core.Plugin;

/// <summary>
/// 插件基类，提供 <see cref="IPlugin"/> 接口的默认实现。
/// </summary>
/// <remarks>
/// 此类提供了插件的基本实现，包括：
/// <list type="bullet">
///   <item><description>状态管理：自动管理插件状态转变</description></item>
///   <item><description>生命周期守卫：在错误阶段调用服务时抛出明确异常</description></item>
///   <item><description>自动 Job 加载：从 plugin.yaml 自动注册作业</description></item>
///   <item><description>便捷方法：GetService、GetAsgardContext、CreateLogger 等</description></item>
/// </list>
/// 继承此类时，只需要实现抽象属性和重写需要自定义的方法。
/// </remarks>
/// <example>
/// 插件实现示例：
/// <code>
/// public class MyPlugin : PluginBase
/// {
///     public override string Id => "my-plugin";
///     public override string Name => "我的插件";
///     public override Version Version => new(1, 0, 0);
///
///     protected override Task OnInitializeAsync(CancellationToken cancellationToken)
///     {
///         var logger = CreateLogger();  // 安全：框架已完成阶段转换
///         logger.LogInformation("插件初始化完成");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public abstract class PluginBase : IPlugin
{
    /// <summary>
    /// 生命周期阶段枚举
    /// </summary>
    private enum LifecyclePhase
    {
        /// <summary>构造完成</summary>
        Created,
        /// <summary>InitializeAsync 完成，ServiceProvider 可用</summary>
        Initialized,
        /// <summary>StartAsync 完成</summary>
        Started,
        /// <summary>StopAsync 完成</summary>
        Stopped
    }

    /// <summary>
    /// 当前生命周期阶段
    /// </summary>
    private LifecyclePhase _phase = LifecyclePhase.Created;

    /// <summary>
    /// 插件当前状态
    /// </summary>
    private PluginState _state = PluginState.Unloaded;

    /// <summary>
    /// 获取插件唯一标识
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// 获取插件名称
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 获取插件版本
    /// </summary>
    public abstract Version Version { get; }

    /// <summary>
    /// 获取插件描述
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    /// 获取插件当前状态
    /// </summary>
    public PluginState State
    {
        get => _state;
        protected set => _state = value;
    }

    /// <summary>
    /// 获取插件依赖的其他插件 ID 列表
    /// </summary>
    public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();

    #region 框架设置属性（internal set，仅框架可写）

    /// <summary>
    /// 获取服务提供者。框架在 InitializeAsync 之前设置。
    /// </summary>
    public IServiceProvider ServiceProvider { get; internal set; } = null!;

    /// <summary>
    /// 获取插件数据目录。每个插件拥有独立的子目录。
    /// </summary>
    public string DataDirectory { get; internal set; } = string.Empty;

    /// <summary>
    /// 获取插件所在目录（DLL 所在目录）。
    /// </summary>
    public string PluginDirectory { get; internal set; } = string.Empty;

    /// <summary>
    /// 获取在根服务提供者仍可用时预先创建并缓存的日志器。
    /// </summary>
    internal ILogger? CachedLogger { get; set; }

    #endregion

    #region 便捷方法（带生命周期阶段守卫）
    /// <summary>
    /// 从 DI 容器获取必需服务。
    /// </summary>
    /// <typeparam name="T">服务类型。</typeparam>
    /// <returns>服务实例。</returns>
    /// <exception cref="InvalidOperationException">在 InitializeAsync 之前调用时抛出。</exception>
    protected T GetService<T>() where T : notnull
    {
        EnsureServiceProviderAvailable();
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// 从 DI 容器获取可选服务。
    /// </summary>
    /// <typeparam name="T">服务类型。</typeparam>
    /// <returns>服务实例，未注册时返回 null。</returns>
    protected T? GetOptionalService<T>() where T : class
    {
        EnsureServiceProviderAvailable();
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// 创建当前插件类型的日志器。
    /// </summary>
    /// <returns>日志器实例。</returns>
    protected ILogger CreateLogger()
    {
        if (CachedLogger is not null)
        {
            return CachedLogger;
        }

        EnsureServiceProviderAvailable();
        CachedLogger = ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        return CachedLogger;
    }

    /// <summary>
    /// 获取 Asgard 框架统一上下文，提供 Cache、JobScheduler、MessageQueue 等能力。
    /// </summary>
    /// <returns>Asgard 上下文实例。</returns>
    protected AbsAsgardContext GetAsgardContext()
    {
        EnsureServiceProviderAvailable();
        return ServiceProvider.GetRequiredService<AbsAsgardContext>();
    }

    /// <summary>
    /// 确保 ServiceProvider 可用（已过 InitializeAsync 阶段）。
    /// </summary>
    /// <exception cref="InvalidOperationException">在 InitializeAsync 之前调用时抛出。</exception>
    private void EnsureServiceProviderAvailable()
    {
        if (_phase < LifecyclePhase.Initialized)
        {
            throw new InvalidOperationException(
                $"插件 '{Id}' 在 {_phase} 阶段调用了 GetService()。" +
                "ServiceProvider 仅在 InitializeAsync 及之后的生命周期阶段可用。");
        }
    }

    #endregion

    #region 生命周期方法（模板方法 — 子类重写 OnXxxAsync）
    /// <summary>
    /// 配置插件服务。子类重写 <see cref="OnConfigureServicesAsync"/> 实现自定义逻辑。
    /// </summary>
    public Task ConfigureServicesAsync(IPluginServiceConfigurationContext context, CancellationToken cancellationToken = default)
    {
        return OnConfigureServicesAsync(context, cancellationToken);
    }

    /// <summary>
    /// 初始化插件。框架先完成阶段转换，再调用 <see cref="OnInitializeAsync"/>，
    /// 因此子类在 OnInitializeAsync 中可安全使用 GetService / CreateLogger 等方法。
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _phase = LifecyclePhase.Initialized;
        State = PluginState.Initialized;
        await OnInitializeAsync(cancellationToken);
    }

    /// <summary>
    /// 启动插件。框架自动从 plugin.yaml 加载作业，并在完成后设置阶段。
    /// 子类重写 <see cref="OnStartAsync"/> 实现自定义启动逻辑。
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await OnStartAsync(cancellationToken);
        await AutoRegisterJobsAsync(cancellationToken);
        _phase = LifecyclePhase.Started;
        State = PluginState.Running;
    }

    /// <summary>
    /// 停止插件。子类重写 <see cref="OnStopAsync"/> 实现自定义停止逻辑。
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await OnStopAsync(cancellationToken);
        _phase = LifecyclePhase.Stopped;
        State = PluginState.Stopped;
    }

    /// <summary>
    /// 释放插件资源。子类重写 <see cref="OnDisposeAsync"/> 实现自定义释放逻辑。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
        State = PluginState.Unloaded;
    }

    /// <summary>
    /// 配置插件中间件。子类重写 <see cref="OnConfigureMiddlewareAsync"/> 实现自定义逻辑。
    /// </summary>
    public Task ConfigureMiddlewareAsync(IPluginMiddlewareConfigurationContext context, CancellationToken cancellationToken = default)
    {
        return OnConfigureMiddlewareAsync(context, cancellationToken);
    }

    #endregion

    #region 子类钩子（protected virtual）

    /// <summary>
    /// 配置插件服务的钩子。子类重写此方法注册自定义服务。
    /// </summary>
    protected virtual Task OnConfigureServicesAsync(IPluginServiceConfigurationContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 初始化插件的钩子。此时 GetService / CreateLogger 等方法已可用。
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 启动插件的钩子。子类重写此方法实现自定义启动逻辑。
    /// </summary>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止插件的钩子。子类重写此方法实现自定义停止逻辑。
    /// </summary>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 释放插件资源的钩子。子类重写此方法释放自定义资源。
    /// </summary>
    protected virtual ValueTask OnDisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 配置插件中间件的钩子。子类重写此方法注册自定义中间件。
    /// </summary>
    protected virtual Task OnConfigureMiddlewareAsync(IPluginMiddlewareConfigurationContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    /// <summary>
    /// 自动从 plugin.yaml 加载并注册作业。
    /// </summary>
    private async Task AutoRegisterJobsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(PluginDirectory)) return;

        var yamlPath = Path.Combine(PluginDirectory, "plugin.yaml");
        if (!File.Exists(yamlPath)) return;

        var jobManager = GetOptionalService<IJobManager>();
        if (jobManager == null) return;

        var jobConfig = YamlConfigLoader.LoadFromFile<PluginJobConfig>(yamlPath);
        if (jobConfig.Jobs.Count == 0) return;

        var logger = CreateLogger();
        foreach (var job in jobConfig.Jobs)
        {
            await jobManager.Scheduler.ScheduleJobAsync(job, cancellationToken);
            logger.LogInformation("插件 {PluginId} 已自动注册作业: {Group}:{Name}",
                Id, job.Group, job.Name);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Name} v{Version} ({Id}) [{State}]";
    }
}
