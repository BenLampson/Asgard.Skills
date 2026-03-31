namespace Asgard.PluginSdk;

/// <summary>
/// 为 Asgard 插件提供基于约定的注册与配置加载辅助方法。
/// </summary>
/// <remarks>
/// 这些辅助方法统一了插件启动时的常见工作，使插件包能够在 Asgard 生态中遵循一致的
/// 程序集扫描和配置加载行为。
/// </remarks>
public static class PluginConventions
{
    private const string _pluginManifestFileName = "plugin.yaml";

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

    /// <summary>
    /// 从指定插件类型所在程序集目录加载插件配置。
    /// </summary>
    /// <typeparam name="TPlugin">用于定位插件程序集目录的插件类型。</typeparam>
    /// <typeparam name="TConfig">从 <c>plugin.yaml</c> 读取的强类型插件配置类型。</typeparam>
    /// <returns>加载得到的配置实例；如果清单文件不存在，则返回新的默认实例。</returns>
    /// <remarks>
    /// 当配置需要在没有插件实例的情况下解析时，使用此重载。
    /// </remarks>
    public static TConfig LoadPluginConfig<TPlugin, TConfig>()
        where TPlugin : PluginBase
        where TConfig : class, ISystemConfig, new()
    {
        return LoadPluginConfig<TConfig>(typeof(TPlugin).Assembly);
    }

    /// <summary>
    /// 从当前插件实例所在目录加载插件配置。
    /// </summary>
    /// <typeparam name="TConfig">从 <c>plugin.yaml</c> 读取的强类型插件配置类型。</typeparam>
    /// <param name="plugin">其 <see cref="PluginBase.PluginDirectory"/> 将作为配置根目录的插件实例。</param>
    /// <returns>加载得到的配置实例；如果清单文件不存在，则返回新的默认实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="plugin"/> 为 <see langword="null" /> 时抛出。</exception>
    public static TConfig LoadPluginConfig<TConfig>(this PluginBase plugin)
        where TConfig : class, ISystemConfig, new()
    {
        ArgumentNullException.ThrowIfNull(plugin);
        return LoadPluginConfig<TConfig>(plugin.PluginDirectory);
    }

    /// <summary>
    /// 从指定程序集所在目录加载插件配置。
    /// </summary>
    /// <typeparam name="TConfig">从 <c>plugin.yaml</c> 读取的强类型插件配置类型。</typeparam>
    /// <param name="assembly">用于解析插件目录的程序集。</param>
    /// <returns>加载得到的配置实例；如果清单文件不存在，则返回新的默认实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="assembly"/> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当无法从 <paramref name="assembly" /> 解析插件目录时抛出。</exception>
    public static TConfig LoadPluginConfig<TConfig>(Assembly assembly)
        where TConfig : class, ISystemConfig, new()
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var pluginDirectory = Path.GetDirectoryName(assembly.Location)
            ?? throw new InvalidOperationException($"Unable to resolve plugin directory for {assembly.FullName}.");

        return LoadPluginConfig<TConfig>(pluginDirectory);
    }

    /// <summary>
    /// 从指定插件目录加载插件配置。
    /// </summary>
    /// <typeparam name="TConfig">从 <c>plugin.yaml</c> 读取的强类型插件配置类型。</typeparam>
    /// <param name="pluginDirectory">可能包含 <c>plugin.yaml</c> 清单文件的插件根目录。</param>
    /// <returns>加载得到的配置实例；如果清单文件不存在，则返回新的默认实例。</returns>
    /// <remarks>
    /// 当清单文件缺失时，此方法会返回新的 <typeparamref name="TConfig" /> 实例而不是直接失败，
    /// 这样可以让插件在可选配置场景下保持轻量启动。
    /// </remarks>
    /// <exception cref="ArgumentException">当 <paramref name="pluginDirectory"/> 为 <see langword="null" />、空字符串或空白时抛出。</exception>
    public static TConfig LoadPluginConfig<TConfig>(string pluginDirectory)
        where TConfig : class, ISystemConfig, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);

        var manifestPath = Path.Combine(pluginDirectory, _pluginManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return new TConfig();
        }

        return YamlConfigLoader.LoadFromFile<TConfig>(manifestPath);
    }
}
