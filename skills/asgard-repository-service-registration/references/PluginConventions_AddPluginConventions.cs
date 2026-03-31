namespace Asgard.PluginSdk;

/// <summary>
/// 为 Asgard 插件提供基于约定的注册与配置加载辅助方法。
/// </summary>
/// <remarks>
/// 这些辅助方法统一了插件启动时的常见工作，使插件包能够在 Asgard 生态中遵循一致的
/// 程序集扫描和配置加载行为。
/// </remarks>
public static partial class PluginConventions
{
    /// <summary>
    /// 注册插件程序集内发现的仓储与服务类型，并将插件配置作为单例加载到容器中。
    /// </summary>
    /// <typeparam name="TPlugin">其所在程序集会被扫描依赖项的插件类型。</typeparam>
    /// <typeparam name="TConfig">从 <c>plugin.yaml</c> 读取的强类型插件配置类型。</typeparam>
    /// <param name="context">插件启动期间使用的服务配置上下文。</param>
    /// <returns>加载得到的配置实例；如果清单文件不存在，则返回新的默认实例。</returns>
    /// <remarks>
    /// 此方法会扫描插件程序集中的仓储与服务注册，然后加载插件清单配置并将其作为单例注入容器。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="context"/> 为 <see langword="null" /> 时抛出。</exception>
    public static TConfig AddPluginConventions<TPlugin, TConfig>(this IPluginServiceConfigurationContext context)
        where TPlugin : PluginBase
        where TConfig : class, ISystemConfig, new()
    {
        ArgumentNullException.ThrowIfNull(context);

        var assembly = typeof(TPlugin).Assembly;

        _ = context.Services.AddRepositories(assembly);
        _ = context.Services.AddServices(assembly);

        var config = LoadPluginConfig<TPlugin, TConfig>();
        _ = context.Services.AddSingleton(config);

        return config;
    }
}
