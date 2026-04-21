namespace Asgard.Abstractions.Identity;

public interface IAsgardIdentityContext
{
    AsgardIdentitySnapshot Current { get; }

    Guid GetCurrentTenantId();

    AbsAsgardUserInfo? UserInfo { get; }

    UserType UserType { get; }

    TokenType TokenType { get; }
}
