namespace Asgard.AspNetCore.Core.Identity;

public class DefaultAsgardIdentityContextResolver : IAsgardIdentityContextResolver
{
    public AsgardIdentitySnapshot ResolveSnapshot(HttpContext context)
    {
        DefaultAsgardUserInfo? userInfo = null;
        var tenantId = Guid.Empty;

        if (context.User?.Claims is not null)
        {
            userInfo = new DefaultAsgardUserInfo();
            userInfo.InitFromClaims(context.User.Claims);

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

    private static bool TryResolveExplicitTokenType(IEnumerable<Claim> claims, out TokenType tokenType)
    {
        foreach (var claimName in AsgardClaimTypes.TokenTypeAliases)
        {
            var claimValue = claims.FirstOrDefault(claim => string.Equals(claim.Type, claimName, StringComparison.OrdinalIgnoreCase))?.Value;
            if (TryMapTokenType(claimValue, out tokenType))
            {
                return true;
            }
        }

        tokenType = default;
        return false;
    }

    private static bool HasServiceClientClaim(IEnumerable<Claim> claims)
    {
        return claims.Any(claim =>
            AsgardClaimTypes.ClientIdAliases.Any(name => string.Equals(claim.Type, name, StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(claim.Value));
    }

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

        var normalizedValue = claimValue.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return normalizedValue switch
        {
            "backendservice" or "service" => (tokenType = TokenType.BackendService) == TokenType.BackendService,
            "userlogin" or "user" => (tokenType = TokenType.UserLogin) == TokenType.UserLogin,
            _ => false
        };
    }
}
