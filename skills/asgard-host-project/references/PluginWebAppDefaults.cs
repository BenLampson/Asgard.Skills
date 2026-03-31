namespace Asgard.PluginSdk;

/// <summary>
/// 为运行在 Yggdrasil 上的 Asgard 插件提供推荐的 ASP.NET Core 启动默认值。
/// </summary>
/// <remarks>
/// 这些辅助方法集中定义了插件宿主 Web 应用推荐使用的默认中间件顺序，
/// 让插件包开箱即可与宿主管道保持一致。
/// </remarks>
public static class PluginWebAppDefaults
{
    /// <summary>
    /// 为插件 Web 应用应用推荐的中间件管道。
    /// </summary>
    /// <param name="app">用于配置中间件的应用构建器。</param>
    /// <returns>可继续链式调用的同一个 <see cref="IApplicationBuilder"/> 实例。</returns>
    /// <remarks>
    /// 当前默认管道会启用 Asgard 异常处理器和 HTTPS 重定向。
    /// 静态文件、CORS、认证、授权以及身份上下文等由宿主管理的能力，仍由 Yggdrasil 自身负责接线。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="app"/> 为 <see langword="null" /> 时抛出。</exception>
    public static IApplicationBuilder UseRecommendedPluginDefaults(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseAsgardExceptionHandler()
            .UseHttpsRedirection();
    }

    /// <summary>
    /// 使用内建插件和推荐中间件默认值构建并运行 Yggdrasil 宿主。
    /// </summary>
    /// <typeparam name="TPlugin">要注册为内建插件的插件类型。</typeparam>
    /// <param name="configPath">传递给宿主构建器的应用配置文件路径。</param>
    /// <param name="configure">用于在推荐默认值之后追加额外中间件的可选回调。</param>
    /// <returns>当宿主关闭时完成的任务。</returns>
    /// <remarks>
    /// 此重载适用于希望通过单一入口完成 Yggdrasil 宿主创建、内建插件注册和推荐中间件配置的插件项目。
    /// </remarks>
    public static Task RunAsync<TPlugin>(string configPath = "config/app.yaml", Action<IApplicationBuilder>? configure = null)
        where TPlugin : class, IPlugin
    {
        var builder = YggdrasilHost.CreateBuilder(configPath)
            .UseBuiltInPlugin<TPlugin>()
            .ConfigureMiddleware(app =>
            {
                _ = app.UseRecommendedPluginDefaults();
                configure?.Invoke(app);
            });

        var app = builder.Build();
        return app.RunAsync();
    }

    /// <summary>
    /// 使用推荐中间件默认值构建并运行 Yggdrasil 宿主。
    /// </summary>
    /// <param name="configPath">传递给宿主构建器的应用配置文件路径。</param>
    /// <param name="configure">用于在推荐默认值之后追加额外中间件的可选回调。</param>
    /// <returns>当宿主关闭时完成的任务。</returns>
    /// <remarks>
    /// 当插件注册在其他位置处理，而这里只需要套用标准宿主与中间件接线时，使用此重载。
    /// </remarks>
    public static Task RunAsync(string configPath = "config/app.yaml", Action<IApplicationBuilder>? configure = null)
    {
        var builder = YggdrasilHost.CreateBuilder(configPath)
            .ConfigureMiddleware(app =>
            {
                _ = app.UseRecommendedPluginDefaults();
                configure?.Invoke(app);
            });

        var app = builder.Build();
        return app.RunAsync();
    }
}
