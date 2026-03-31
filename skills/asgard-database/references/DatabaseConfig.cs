namespace Asgard.Abstractions.Data;


/// <summary>
/// 数据库配置类，用于配置 FreeSQL 数据库连接。
/// </summary>
/// <remarks>
/// <para>此类包含数据库类型和连接字符串等配置信息。</para>
/// <para>支持的数据库类型：SqlServer、PostgreSQL、MySQL、SQLite 等。</para>
/// </remarks>
/// <example>
/// <code>
/// # 配置示例
/// database:
///   enabled: true
///   provider: SqlServer
///   connectionString: "Server=localhost;Database=AsgardDB;User Id=sa;Password=your_password;"
/// </code>
/// </example>
public class DatabaseConfig : ISystemConfig
{
    /// <summary>
    /// 获取或设置是否启用数据库模块。
    /// </summary>
    /// <remarks>默认为 false，表示禁用数据库模块。</remarks>
    [ConfigPath("database.enabled", DefaultValue = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 获取或设置数据库提供者类型。
    /// </summary>
    /// <remarks>
    /// 支持的值：SqlServer、PostgreSQL、MySQL、SQLite、Oracle、达梦、金仓等。
    /// 对应 FreeSQL 的 DataType 枚举。
    /// </remarks>
    [ConfigPath("database.provider", DefaultValue = "MySQL")]
    public string Provider { get; set; } = "MySQL";

    /// <summary>
    /// 获取或设置数据库连接字符串。
    /// </summary>
    /// <remarks>
    /// 根据选择的数据库类型，提供相应格式的连接字符串。
    /// </remarks>
    [ConfigPath("database.connectionString", DefaultValue = "")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 验证配置的有效性。
    /// </summary>
    /// <remarks>
    /// 如果启用了数据库模块，则必须提供有效的数据库类型和连接字符串。
    /// </remarks>
    /// <exception cref="InvalidOperationException">当配置无效时抛出。</exception>
    public void Validate()
    {
        if (Enabled)
        {
            if (string.IsNullOrEmpty(Provider))
            {
                throw new InvalidOperationException("数据库提供者类型不能为空");
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new InvalidOperationException("数据库连接字符串不能为空");
            }
        }
    }
}
