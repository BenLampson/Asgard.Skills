namespace Asgard.Abstractions.Identity;

public sealed record AsgardIdentitySnapshot(
    Guid TenantId,
    AbsAsgardUserInfo? UserInfo,
    UserType UserType,
    TokenType TokenType)
{
    public static AsgardIdentitySnapshot Empty { get; } =
        new(Guid.Empty, null, default, default);
}
