namespace Asgard.Abstractions.Identity;

/// <summary>
/// 统一定义 Asgard 身份体系中使用的标准 Claim 名称。
/// </summary>
public static class AsgardClaimTypes
{
    /// <summary>
    /// 主体标识 Claim。
    /// </summary>
    public const string Sub = "sub";

    /// <summary>
    /// 业务用户标识 Claim。
    /// </summary>
    public const string UserId = "user_id";

    /// <summary>
    /// 租户标识 Claim。
    /// </summary>
    public const string TenantId = "tenant_id";

    /// <summary>
    /// 后端服务调用方标识 Claim。
    /// </summary>
    public const string ClientId = "client_id";

    /// <summary>
    /// 角色列表 Claim。
    /// </summary>
    public const string Roles = "roles";

    /// <summary>
    /// 权限列表 Claim。
    /// </summary>
    public const string Permissions = "permissions";

    /// <summary>
    /// 作用域列表 Claim。
    /// </summary>
    public const string Scope = "scope";

    /// <summary>
    /// 用户元数据 Claim。
    /// </summary>
    public const string UserMetadatas = "userMetadatas";

    /// <summary>
    /// 租户元数据 Claim。
    /// </summary>
    public const string TenantMetadata = "tenantMetadata";

    /// <summary>
    /// 令牌类型 Claim。
    /// </summary>
    public const string TokenType = "token_type";
}
