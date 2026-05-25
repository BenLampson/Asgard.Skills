namespace Asgard.Abstractions.AspNetCore.Authorization;

/// <summary>
/// 为全部 <c>AsgardAuth*</c> 授权特性提供统一的基础实现。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class AsgardAuthAttributeBase : AuthorizeAttribute, IAsgardAuthMetadata
{
    /// <summary>
    /// 初始化授权特性基类。
    /// </summary>
    protected AsgardAuthAttributeBase()
    {
        Policy = AsgardAuthConstants.PolicyName;
    }

    /// <inheritdoc />
    public abstract AsgardAuthMetadataKind Kind { get; }

    /// <inheritdoc />
    public virtual string? Key => null;

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Values => Array.Empty<string>();

    /// <inheritdoc />
    public virtual string? Expression => null;
}

/// <summary>
/// 显式使用逻辑与连接相邻的 Asgard 授权条件。
/// </summary>
public sealed class AsgardAuthAndAttribute : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.And;
}

/// <summary>
/// 显式使用逻辑或连接相邻的 Asgard 授权条件。
/// </summary>
public sealed class AsgardAuthOrAttribute : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.Or;
}

/// <summary>
/// 要求当前用户至少具备指定角色中的任意一个。
/// </summary>
public sealed class AsgardAuthAnyRoleAttribute(params string[] roles) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.AnyRole;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => roles ?? [];
}

/// <summary>
/// 要求当前用户同时具备指定的全部角色。
/// </summary>
public sealed class AsgardAuthAllRoleAttribute(params string[] roles) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.AllRole;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => roles ?? [];
}

/// <summary>
/// 要求当前用户至少具备指定权限中的任意一个。
/// </summary>
public sealed class AsgardAuthAnyPermissionAttribute(params string[] permissions) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.AnyPermission;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => permissions ?? [];
}

/// <summary>
/// 要求当前用户同时具备指定的全部权限。
/// </summary>
public sealed class AsgardAuthAllPermissionAttribute(params string[] permissions) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.AllPermission;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => permissions ?? [];
}

/// <summary>
/// 要求当前用户至少具备指定范围中的任意一个。
/// </summary>
public sealed class AsgardAuthAnyScopeAttribute(params string[] scopes) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.AnyScope;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => scopes ?? [];
}

/// <summary>
/// 要求当前用户同时具备指定的全部范围。
/// </summary>
public sealed class AsgardAuthAllScopeAttribute(params string[] scopes) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.AllScope;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => scopes ?? [];
}

/// <summary>
/// 要求用户元数据中的指定键等于给定值。
/// </summary>
public sealed class AsgardAuthUserMetadataEqualsAttribute(string key, string value) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.UserMetadataEquals;

    /// <inheritdoc />
    public override string? Key => key;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => [value];
}

/// <summary>
/// 要求用户元数据中的指定键匹配给定值集合中的任意一个。
/// </summary>
public sealed class AsgardAuthUserMetadataInAttribute(string key, params string[] values) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.UserMetadataIn;

    /// <inheritdoc />
    public override string? Key => key;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => values ?? [];
}

/// <summary>
/// 要求租户元数据中的指定键等于给定值。
/// </summary>
public sealed class AsgardAuthTenantMetadataEqualsAttribute(string key, string value) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.TenantMetadataEquals;

    /// <inheritdoc />
    public override string? Key => key;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => [value];
}

/// <summary>
/// 要求租户元数据中的指定键匹配给定值集合中的任意一个。
/// </summary>
public sealed class AsgardAuthTenantMetadataInAttribute(string key, params string[] values) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.TenantMetadataIn;

    /// <inheritdoc />
    public override string? Key => key;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => values ?? [];
}

/// <summary>
/// 要求指定的 Asgard 授权 DSL 表达式计算结果为 <see langword="true" />。
/// </summary>
public sealed class AsgardAuthMatchAttribute(string expression) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.Match;

    /// <inheritdoc />
    public override string? Expression => expression;
}

/// <summary>
/// 要求当前用户显示名包含指定文本。
/// </summary>
public sealed class AsgardAuthNameLikeAttribute(string value) : AsgardAuthAttributeBase
{
    /// <inheritdoc />
    public override AsgardAuthMetadataKind Kind => AsgardAuthMetadataKind.NameLike;

    /// <inheritdoc />
    public override IReadOnlyList<string> Values => [value];
}
