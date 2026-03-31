namespace Asgard.Yggdrasil.AspNetCore;

/// <summary>
/// Yggdrasil 主机静态入口类，提供 ASP.NET Core 应用的启动编排能力。
/// </summary>
/// <remarks>
/// <para>
/// 此类作为 Asgard 框架的统一启动入口，简化了 ASP.NET Core 应用的配置和启动过程。
/// 通过此类，用户可以用一行代码启动应用，同时保留扩展能力。
/// </para>
/// <para>
/// <b>启动流程：</b>
/// <list type="number">
///   <item>配置加载阶段：加载 YAML 配置、验证配置完整性</item>
///   <item>模块初始化阶段：解析模块依赖、按拓扑顺序注册模块</item>
///   <item>ASP.NET 主机阶段：配置 Kestrel、配置中间件管道</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <b>最简启动：</b>
/// <code>
/// await YggdrasilHost.CreateBuilder("config/app.yaml").Build().RunAsync();
/// </code>
/// </example>
/// <example>
/// <b>带模块配置启动：</b>
/// <code>
/// await YggdrasilHost.CreateBuilder("config/app.yaml")
///     .ConfigureModules(options => 
///     {
///         options.EnableCaching = true;
///         options.EnableMessageQueue = false;
///     })
///     .Build()
///     .RunAsync();
/// </code>
/// </example>
/// <example>
/// <b>带钩子启动：</b>
/// <code>
/// await YggdrasilHost.CreateBuilder("config/app.yaml")
///     .BeforeModuleRegistration(services => 
///     {
///         services.AddScoped&lt;IMyService, MyService&gt;();
///     })
///     .ConfigureMiddleware(app => 
///     {
///         app.UseMyCustomMiddleware();
///     })
///     .Build()
///     .RunAsync();
/// </code>
/// </example>
public static class YggdrasilHost
{
    /// <summary>
    /// 创建 Yggdrasil 主机构建器。
    /// </summary>
    /// <param name="configFilePath">配置文件路径，相对于应用程序根目录。</param>
    /// <returns>Yggdrasil 主机构建器实例。</returns>
    /// <remarks>
    /// <para>
    /// 此方法是 Asgard 应用的入口点，返回一个 <see cref="YggdrasilHostBuilder"/> 实例，
    /// 用户可以通过链式调用配置模块、添加钩子，最终调用 <see cref="YggdrasilHostBuilder.Build"/> 构建应用。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>Web API 应用</item>
    ///   <item>Worker Service 应用</item>
    ///   <item>需要模块化架构的应用</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = YggdrasilHost.CreateBuilder("config/app.yaml");
    /// var app = builder.Build();
    /// await app.RunAsync();
    /// </code>
    /// </example>
    public static YggdrasilHostBuilder CreateBuilder(string configFilePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(configFilePath);
        
        // 检查是否为空白字符串
        if (string.IsNullOrWhiteSpace(configFilePath))
        {
            throw new ArgumentException("配置文件路径不能为空白字符串", nameof(configFilePath));
        }
        
        return new YggdrasilHostBuilder(configFilePath);
    }
}
