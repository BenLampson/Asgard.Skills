namespace Asgard.Abstractions.Plugin;

/// <summary>
/// 插件中间件配置上下文
/// </summary>
/// <remarks>
/// 提供插件在中间件配置阶段所需的上下文信息，
/// 允许插件向ASP.NET Core管道中注册中间件。
/// </remarks>
public interface IPluginMiddlewareConfigurationContext
{
    /// <summary>
    /// 应用程序构建器
    /// </summary>
    /// <remarks>
    /// 用于向ASP.NET Core中间件管道中添加中间件。
    /// </remarks>
    /// <value>应用程序构建器实例。</value>
    IApplicationBuilder App { get; }

    /// <summary>
    /// 服务提供者
    /// </summary>
    /// <remarks>
    /// 已构建的服务提供者，可用于解析已注册的服务。
    /// </remarks>
    /// <value>服务提供者实例。</value>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 日志记录器
    /// </summary>
    /// <remarks>
    /// 用于记录中间件配置过程中的日志信息。
    /// </remarks>
    /// <value>日志记录器实例。</value>
    ILogger Logger { get; }
}
