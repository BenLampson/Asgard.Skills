namespace Asgard.Abstractions.Plugin;

/// <summary>
/// 插件服务配置上下文接口，在服务注册阶段提供给插件使用。
/// </summary>
/// <remarks>
/// 此上下文在主机构建之前可用，仅提供服务注册能力，不提供 ServiceProvider。
/// 插件应在此上下文中注册自己的仓储、业务服务等。
/// </remarks>
/// <example>
/// 使用示例：
/// <code>
/// public class MyPlugin : IPlugin
/// {
///     public async Task ConfigureServicesAsync(IPluginServiceConfigurationContext context, CancellationToken cancellationToken = default)
///     {
///         // 注册仓储
///         context.Services.AddRepositories(typeof(MyPlugin).Assembly);
///         
///         // 注册业务服务
///         context.RegisterServices(services =>
///         {
///             services.AddScoped&lt;IMyService, MyService&gt;();
///         });
///     }
/// }
/// </code>
/// </example>
public interface IPluginServiceConfigurationContext
{
    /// <summary>
    /// 获取服务集合，用于注册插件服务。
    /// </summary>
    /// <value>服务集合实例。</value>
    IServiceCollection Services { get; }

    /// <summary>
    /// 获取日志器。
    /// </summary>
    /// <value>日志器实例。</value>
    ILogger Logger { get; }

    /// <summary>
    /// 注册服务到服务容器。
    /// </summary>
    /// <param name="configure">服务配置委托。</param>
    void RegisterServices(Action<IServiceCollection> configure);
}
