namespace Asgard.Abstractions.SystemConfig;

/// <summary>
/// 配置路径特性，用于标注配置类中的属性与配置文件中路径的映射关系。
/// </summary>
/// <remarks>
/// 此特性只能应用于属性，每个属性只能应用一次。
/// 路径使用点号分隔，例如 "database.connection.host" 表示数据库的配置路径。
/// </remarks>
/// <example>
/// 使用示例：
/// <code>
/// public class DatabaseConfig : ISystemConfig
/// {
///     [ConfigPath("database.host", DefaultValue = "localhost")]
///     public string Host { get; set; } = string.Empty;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ConfigPathAttribute : Attribute
{
    /// <summary>
    /// 获取配置在文件中的路径。
    /// </summary>
    /// <value>
    /// 使用点号分隔的路径字符串，例如 "app.name" 或 "database.connection.port"。
    /// </value>
    public string Path { get; }

    private object? _defaultValue;

    /// <summary>
    /// 获取或设置属性的默认值。
    /// </summary>
    /// <value>
    /// 当配置文件中不存在指定路径时使用的默认值，默认为 null。
    /// </value>
    /// <remarks>
    /// 设置默认值后，<see cref="HasDefaultValue"/> 属性会自动设为 true。
    /// </remarks>
    public object? DefaultValue
    {
        get => _defaultValue;
        set
        {
            _defaultValue = value;
            HasDefaultValue = true;
        }
    }

    /// <summary>
    /// 获取一个值，指示是否已设置默认值。
    /// </summary>
    /// <value>
    /// 如果已通过 <see cref="DefaultValue"/> 设置默认值，则为 true，否则为 false。
    /// </value>
    public bool HasDefaultValue { get; private set; }

    /// <summary>
    /// 初始化 <see cref="ConfigPathAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="path">配置在文件中的路径，使用点号分隔。</param>
    /// <exception cref="ArgumentException">当 <paramref name="path"/> 为 null、空字符串或包含空白字符时抛出。</exception>
    public ConfigPathAttribute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Configuration path cannot be null or empty", nameof(path));

        Path = path;
    }
}
