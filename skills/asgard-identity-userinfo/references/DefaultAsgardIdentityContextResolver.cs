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
            TokenType.UserLogin);
    }
}
