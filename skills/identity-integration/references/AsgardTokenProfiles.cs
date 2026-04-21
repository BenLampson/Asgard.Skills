namespace Asgard.Abstractions.Identity;

public static class AsgardTokenProfiles
{
    public static IReadOnlyList<string> UserLoginRequiredClaims { get; } =
    [
        AsgardClaimTypes.Sub,
        AsgardClaimTypes.UserId
    ];

    public static IReadOnlyList<string> UserLoginRecommendedClaims { get; } =
    [
        AsgardClaimTypes.TenantId,
        AsgardClaimTypes.Roles,
        AsgardClaimTypes.Permissions,
        AsgardClaimTypes.Scope,
        AsgardClaimTypes.TokenType
    ];

    public static IReadOnlyList<string> BackendServiceRequiredClaims { get; } =
    [
        AsgardClaimTypes.Sub,
        AsgardClaimTypes.ClientId,
        AsgardClaimTypes.TokenType
    ];

    public static IReadOnlyList<string> BackendServiceRecommendedClaims { get; } =
    [
        AsgardClaimTypes.TenantId,
        AsgardClaimTypes.Scope
    ];
}
