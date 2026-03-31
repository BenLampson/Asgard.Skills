namespace Asgard.Abstractions.Plugin;

/// <summary>
/// 插件接口，所有插件必须实现此接口。
/// </summary>
/// <remarks>
/// 此接口定义了插件的核心生命周期方法：
/// <list type="number">
///   <item><description>InitializeAsync：初始化插件，注册服务</description></item>
///   <item><description>StartAsync：启动插件，开始提供服务</description></item>
///   <item><description>StopAsync：停止插件，停止提供服务</description></item>
///   <item><description>DisposeAsync：释放插件资源</description></item>
/// </list>
/// 插件可以实现依赖关系，通过 <see cref="Dependencies"/> 属性声明。
/// 加载器会确保依赖的插件先于当前插件加载。
/// </remarks>
/// <example>
/// 插件实现示例：
/// <code>
/// public class MyPlugin : IPlugin
/// {
///     public string Id => "my-plugin";
///     public string Name => "我的插件";
///     public Version Version => new(1, 0, 0);
///     public string Description => "这是一个示例插件";
///     public PluginState State { get; private set; }
///     public IReadOnlyList&lt;string&gt; Dependencies => Array.Empty&lt;string&gt;();
///     
///     public async Task InitializeAsync(CancellationToken cancellationToken = default)
///     {
///         // 初始化逻辑
///         State = PluginState.Initialized;
///     }
///     
///     public async Task StartAsync(CancellationToken cancellationToken = default)
///     {
///         // 启动逻辑
///         State = PluginState.Running;
///     }
///     
///     public async Task StopAsync(CancellationToken cancellationToken = default)
///     {
///         // 停止逻辑
///         State = PluginState.Stopped;
///     }
///     
///     public ValueTask DisposeAsync()
///     {
///         // 清理资源
///         State = PluginState.Unloaded;
///         return ValueTask.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IPlugin : IAsyncDisposable
{
    /// <summary>
    /// 获取插件唯一标识。
    /// </summary>
    /// <remarks>
    /// 插件 ID 用于唯一标识插件，在依赖声明中引用。
    /// 建议使用有意义的名称，如 "logging-plugin"、"database-plugin" 等。
    /// </remarks>
    /// <value>插件唯一标识字符串。</value>
    string Id { get; }

    /// <summary>
    /// 获取插件名称。
    /// </summary>
    /// <remarks>
    /// 插件名称用于显示和日志记录，可以是友好的名称。
    /// </remarks>
    /// <value>插件名称。</value>
    string Name { get; }

    /// <summary>
    /// 获取插件版本。
    /// </summary>
    /// <remarks>
    /// 版本号用于标识插件的版本，可用于兼容性检查。
    /// </remarks>
    /// <value>插件版本。</value>
    Version Version { get; }

    /// <summary>
    /// 获取插件描述。
    /// </summary>
    /// <remarks>
    /// 描述用于说明插件的功能和用途。
    /// </remarks>
    /// <value>插件描述。</value>
    string Description { get; }

    /// <summary>
    /// 获取插件当前状态。
    /// </summary>
    /// <remarks>
    /// 状态反映了插件在生命周期中的当前位置。
    /// </remarks>
    /// <value>插件当前状态。</value>
    PluginState State { get; }

    /// <summary>
    /// 获取插件依赖的其他插件 ID 列表。
    /// </summary>
    /// <remarks>
    /// 声明的依赖插件会在当前插件之前加载。
    /// 如果依赖的插件不存在或无法加载，当前插件也将无法加载。
    /// </remarks>
    /// <value>依赖的插件 ID 列表。</value>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// 配置插件服务。
    /// </summary>
    /// <param name="context">插件服务配置上下文，提供服务注册能力。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    /// <remarks>
    /// 服务配置阶段在主机构建之前执行，插件可以在此阶段：
    /// <list type="bullet">
    ///   <item><description>注册仓储服务</description></item>
    ///   <item><description>注册业务服务</description></item>
    ///   <item><description>注册其他依赖注入服务</description></item>
    /// </list>
    /// 此阶段无法获取 ServiceProvider，因为主机尚未构建。
    /// </remarks>
    Task ConfigureServicesAsync(IPluginServiceConfigurationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化插件。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    /// <remarks>
    /// 初始化阶段在主机构建之后执行，框架已设置好 ServiceProvider 等属性。
    /// 插件可以在此阶段：
    /// <list type="bullet">
    ///   <item><description>读取配置</description></item>
    ///   <item><description>分配必要的资源</description></item>
    ///   <item><description>执行启动前的准备工作</description></item>
    /// </list>
    /// 注意：服务注册应在 ConfigureServicesAsync 阶段完成。
    /// </remarks>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动插件。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    /// <remarks>
    /// 启动阶段应该：
    /// <list type="bullet">
    ///   <item><description>开始监听请求</description></item>
    ///   <item><description>启动后台任务</description></item>
    ///   <item><description>连接外部服务</description></item>
    /// </list>
    /// </remarks>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止插件。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    /// <remarks>
    /// 停止阶段应该：
    /// <list type="bullet">
    ///   <item><description>停止接收新请求</description></item>
    ///   <item><description>完成正在处理的请求</description></item>
    ///   <item><description>停止后台任务</description></item>
    /// </list>
    /// </remarks>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 配置插件中间件。
    /// </summary>
    /// <param name="context">插件中间件配置上下文，提供中间件注册能力。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    /// <remarks>
    /// 中间件配置阶段在主机构建之后、运行之前执行，插件可以在此阶段：
    /// <list type="bullet">
    ///   <item><description>向ASP.NET Core管道中注册中间件</description></item>
    ///   <item><description>配置请求处理管道</description></item>
    ///   <item><description>添加端点映射</description></item>
    /// </list>
    /// 此阶段可以获取已构建的ServiceProvider，解析已注册的服务。
    /// 中间件的注册顺序由插件加载顺序决定。
    /// </remarks>
    Task ConfigureMiddlewareAsync(IPluginMiddlewareConfigurationContext context, CancellationToken cancellationToken = default);
}
