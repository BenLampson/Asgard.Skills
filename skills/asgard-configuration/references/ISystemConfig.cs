namespace Asgard.Abstractions.SystemConfig;

/// <summary>
/// 系统配置接口，定义配置对象必须实现的校验方法。
/// </summary>
/// <remarks>
/// 所有配置类都应实现此接口，以提供配置校验功能。
/// 在配置加载完成后，系统会自动调用 <see cref="Validate"/> 方法进行配置校验。
/// </remarks>
/// <example>
/// 实现示例：
/// <code>
/// public class DatabaseConfig : ISystemConfig
/// {
///     public string ConnectionString { get; set; } = string.Empty;
///
///     public void Validate()
///     {
///         if (string.IsNullOrEmpty(ConnectionString))
///             throw new InvalidOperationException("连接字符串不能为空");
///     }
/// }
/// </code>
/// </example>
public interface ISystemConfig
{
    /// <summary>
    /// 校验配置的有效性。
    /// </summary>
    /// <remarks>
    /// 此方法在配置加载完成后自动调用。如果配置无效，应抛出相应的异常。
    /// 常见的校验包括：必填字段检查、格式校验、范围校验等。
    /// </remarks>
    /// <exception cref="InvalidOperationException">当配置无效时抛出。</exception>
    void Validate();
}
