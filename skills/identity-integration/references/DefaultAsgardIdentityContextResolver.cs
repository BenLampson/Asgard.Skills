namespace Asgard.AspNetCore.Core.Identity;

/// <summary>
/// 默认的身份上下文解析器，用于从声明集合中还原身份信息。
/// </summary>
public class DefaultAsgardIdentityContextResolver : IAsgardIdentityContextResolver
{
    /// <inheritdoc />
    public AsgardIdentitySnapshot ResolveSnapshot(HttpContext context)
    {
        var claims = context.User?.Claims?.ToArray();
        DefaultAsgardUserInfo? userInfo = null;
        var tenantId = Guid.Empty;

        if (claims is not null)
        {
            userInfo = new DefaultAsgardUserInfo();
            userInfo.InitFromClaims(claims);

            if (!string.IsNullOrWhiteSpace(userInfo.TenantId) &&
                Guid.TryParse(userInfo.TenantId, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }
        }

        var userType = tenantId == Guid.Empty
            ? UserType.Platform
            : UserType.Tenant;

        return new AsgardIdentitySnapshot(
            tenantId,
            userInfo,
            userType,
            ResolveTokenType(claims, userInfo));
    }

    /// <summary>
    /// 基于官方 Claim 推断当前令牌类型。
    /// </summary>
    /// <param name="claims">当前请求中的声明集合。</param>
    /// <param name="userInfo">已解析出的用户信息。</param>
    /// <returns>推断得到的令牌类型。</returns>
    private static TokenType ResolveTokenType(IEnumerable<Claim>? claims, DefaultAsgardUserInfo? userInfo)
    {
        if (claims is null)
        {
            return TokenType.UserLogin;
        }

        if (TryResolveExplicitTokenType(claims, out var tokenType))
        {
            return tokenType;
        }

        return HasServiceClientClaim(claims) && string.IsNullOrWhiteSpace(userInfo?.UserId)
            ? TokenType.BackendService
            : TokenType.UserLogin;
    }

    /// <summary>
    /// 优先读取令牌内显式声明的类型字段。
    /// </summary>
    /// <param name="claims">声明集合。</param>
    /// <param name="tokenType">解析成功后的令牌类型。</param>
    /// <returns>是否成功解析。</returns>
    private static bool TryResolveExplicitTokenType(IEnumerable<Claim> claims, out TokenType tokenType)
    {
        var claimValue = claims.FirstOrDefault(claim => string.Equals(claim.Type, AsgardClaimTypes.TokenType, StringComparison.Ordinal))?.Value;
        if (TryMapTokenType(claimValue, out tokenType))
        {
            return true;
        }

        tokenType = default;
        return false;
    }

    /// <summary>
    /// 检查令牌是否带有官方后端服务调用方标识。
    /// </summary>
    /// <param name="claims">声明集合。</param>
    /// <returns>存在客户端标识时返回 true。</returns>
    private static bool HasServiceClientClaim(IEnumerable<Claim> claims)
    {
        return claims.Any(claim =>
            string.Equals(claim.Type, AsgardClaimTypes.ClientId, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(claim.Value));
    }

    /// <summary>
    /// 将不同写法的令牌类型字符串映射到枚举值。
    /// </summary>
    /// <param name="claimValue">Claim 原始值。</param>
    /// <param name="tokenType">映射成功后的令牌类型。</param>
    /// <returns>是否映射成功。</returns>
    private static bool TryMapTokenType(string? claimValue, out TokenType tokenType)
    {
        tokenType = default;
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return false;
        }

        if (Enum.TryParse<TokenType>(claimValue, true, out tokenType))
        {
            return true;
        }

        return false;
    }
}
