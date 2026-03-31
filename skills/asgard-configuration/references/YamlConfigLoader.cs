namespace Asgard.Core.SystemConfig;

/// <summary>
/// YAML 配置加载器，提供从 YAML 内容或文件加载配置的功能。
/// </summary>
/// <remarks>
/// 此类使用 YamlDotNet 库解析 YAML 内容，并通过 <see cref="ConfigPathAttribute"/> 特性
/// 将 YAML 路径映射到配置类的属性。支持嵌套路径、默认值、枚举和基本类型的自动转换，
/// 同时支持嵌套配置对象的自动绑定。
/// </remarks>
public static class YamlConfigLoader
{
    /// <summary>
    /// YAML 反序列化器实例，使用默认命名约定并忽略未匹配的属性。
    /// </summary>
    private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// 从 YAML 字符串内容加载配置对象。
    /// </summary>
    /// <typeparam name="T">配置类型，必须实现 <see cref="ISystemConfig"/> 接口并具有无参构造函数。</typeparam>
    /// <param name="yamlContent">YAML 格式的字符串内容。</param>
    /// <returns>填充了配置数据的配置对象实例。</returns>
    /// <remarks>
    /// 此方法会解析 YAML 内容，然后根据属性上的 <see cref="ConfigPathAttribute"/> 特性
    /// 将对应的值绑定到配置对象的属性上。同时会递归绑定嵌套的配置对象。
    /// </remarks>
    /// <example>
    /// 使用示例：
    /// <code>
    /// const string yaml = "app:\n  name: MyApp";
    /// var config = YamlConfigLoader.Load<AppConfig>(yaml);
    /// </code>
    /// </example>
    public static T Load<T>(string yamlContent) where T : class, ISystemConfig, new()
    {
        var yamlData = _yamlDeserializer.Deserialize<object>(yamlContent);
        var config = new T();

        // 仅当根节点被反序列化为字典时才进入绑定流程，其他情况直接返回默认配置对象。
        if (yamlData is Dictionary<object, object> dict)
        {
            YamlConfigBinder.BindConfig(config, dict);
        }

        return config;
    }

    /// <summary>
    /// 从 YAML 文件加载配置对象。
    /// </summary>
    /// <typeparam name="T">配置类型，必须实现 <see cref="ISystemConfig"/> 接口并具有无参构造函数。</typeparam>
    /// <param name="filePath">YAML 配置文件的完整路径。</param>
    /// <returns>填充了配置数据的配置对象实例。</returns>
    /// <exception cref="FileNotFoundException">当指定的文件不存在时抛出。</exception>
    /// <example>
    /// 使用示例：
    /// <code>
    /// var config = YamlConfigLoader.LoadFromFile<AppConfig>("config.yaml");
    /// </code>
    /// </example>
    public static T LoadFromFile<T>(string filePath) where T : class, ISystemConfig, new()
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"配置文件未找到：{filePath}");

        var yamlContent = File.ReadAllText(filePath);
        return Load<T>(yamlContent);
    }

    /// <summary>
    /// 将 YAML 数据绑定到配置对象的属性上。
    /// </summary>
    /// <typeparam name="T">配置类型，必须实现 <see cref="ISystemConfig"/> 接口。</typeparam>
    /// <param name="config">要绑定数据的配置对象实例。</param>
    /// <param name="yamlData">解析后的 YAML 数据字典。</param>
    /// <remarks>
    /// 此方法会遍历配置类型的所有公共属性：
    /// <list type="bullet">
    ///   <item><description>对于带有 <see cref="ConfigPathAttribute"/> 特性的属性，根据路径从 YAML 数据中获取值。</description></item>
    ///   <item><description>对于复杂类型属性（非字符串、非枚举、非基元类型），递归绑定嵌套对象。</description></item>
    /// </list>
    /// </remarks>
    public static void BindConfig<T>(T config, Dictionary<object, object> yamlData) where T : class, ISystemConfig
    {
        YamlConfigBinder.BindConfig(config, yamlData);
    }

    internal static Dictionary<object, object> LoadDictionary(string yamlContent)
    {
        var yamlData = _yamlDeserializer.Deserialize<object>(yamlContent);
        // 统一把非字典根节点折叠为空字典，减少上层对 YAML 根结构的判空分支。
        return yamlData as Dictionary<object, object> ?? [];
    }

    internal static Dictionary<object, object> LoadDictionaryFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"配置文件未找到：{filePath}");
        }

        var yamlContent = File.ReadAllText(filePath);
        return LoadDictionary(yamlContent);
    }
}
