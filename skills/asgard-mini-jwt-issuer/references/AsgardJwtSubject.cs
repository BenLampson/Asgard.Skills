namespace Asgard.Heimdall.JwtSigning;

/// <summary>
/// 表示待签发的 Asgard JWT 主体。
/// </summary>
public sealed class AsgardJwtSubject
{
    /// <summary>
    /// 获取或设置主体标识。
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 获取或设置业务用户标识。
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 获取或设置租户标识。
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// 获取或设置客户端标识。
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 获取或设置令牌主体类型。
    /// </summary>
    public string? TokenType { get; set; }

    /// <summary>
    /// 获取或设置角色集合。
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; set; } = [];

    /// <summary>
    /// 获取或设置权限集合。
    /// </summary>
    public IReadOnlyCollection<string> Permissions { get; set; } = [];

    /// <summary>
    /// 获取或设置作用域集合。
    /// </summary>
    public IReadOnlyCollection<string> Scope { get; set; } = [];

    /// <summary>
    /// 获取或设置用户元数据。
    /// </summary>
    public IReadOnlyDictionary<string, string> UserMetadatas { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// 获取或设置租户元数据。
    /// </summary>
    public IReadOnlyDictionary<string, string> TenantMetadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// 获取或设置展示名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置邮箱。
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 获取或设置手机号。
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 获取或设置认证时间。
    /// </summary>
    public DateTimeOffset? AuthenticationTime { get; set; }

    /// <summary>
    /// 获取或设置会话标识。
    /// </summary>
    public string? SessionId { get; set; }
}
