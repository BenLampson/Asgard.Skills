// 关联 AsgardTenantAccess / ICrossTenantScopeAuthorizer 契约见 ../../asgard-database/references/shared-entity-cache-tenant-scopes.md。
// 源码核对基线：Asgard 0abb1d959418c4b877ef3fc909abdee553e7b11f；使用时确认目标版本。
namespace Asgard.Abstractions.Identity;

/// <summary>表示当前执行流中的身份快照，租户数据权限独立于用户类型。</summary>
/// <param name="TenantId">当前租户标识；空值表示未建立租户访问范围。</param>
/// <param name="UserInfo">当前用户信息。</param>
/// <param name="UserType">当前用户类型，不代表跨租户权限。</param>
/// <param name="TokenType">当前令牌类型。</param>
public sealed record AsgardIdentitySnapshot(
    Guid TenantId,
    AbsAsgardUserInfo? UserInfo,
    UserType UserType,
    TokenType TokenType)
{
    private Guid _tenantId = TenantId;
    private AsgardTenantAccess _tenantAccess = AsgardTenantAccess.FromTenantId(TenantId);

    /// <summary>获取或初始化租户标识；显式修改租户会清除之前的跨租户权限。</summary>
    public Guid TenantId
    {
        get => _tenantId;
        init
        {
            _tenantId = value;
            _tenantAccess = AsgardTenantAccess.FromTenantId(value);
        }
    }

    /// <summary>获取不可通过用户类型、声明或对象初始化器提升的租户数据访问范围。</summary>
    public AsgardTenantAccess TenantAccess => _tenantAccess;

    /// <summary>表示不包含租户、用户和租户数据权限的空快照。</summary>
    public static AsgardIdentitySnapshot Empty { get; } = new(Guid.Empty, null, default, default);

    /// <summary>通过可信服务端授权器创建跨租户快照，不接受客户端授权布尔值。</summary>
    /// <param name="authorizer">执行当前身份权限校验的服务端授权器。</param>
    /// <returns>授权成功后的跨租户快照。</returns>
    public AsgardIdentitySnapshot WithCrossTenantAccess(ICrossTenantScopeAuthorizer authorizer)
    {
        ArgumentNullException.ThrowIfNull(authorizer);
        authorizer.Authorize(this);
        return this with { TenantId = Guid.Empty, _tenantAccess = AsgardTenantAccess.CrossTenant };
    }
}
