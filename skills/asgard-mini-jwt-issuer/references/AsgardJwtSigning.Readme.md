# Asgard.Heimdall.JwtSigning

Lightweight JWT signing for small Asgard-based projects.

This package is for projects that use the Asgard framework but do not want to hand-build JWT claims. It signs access tokens that follow the Asgard identity claim contract from `Asgard.Abstractions.Identity`.

## What It Does

- Issues JWT access tokens with Asgard-compatible claims.
- Uses `AsgardClaimTypes` from `Asgard.Abstractions`.
- Supports user tokens and backend service tokens.
- Supports RSA signing and symmetric signing.
- Produces JWKS metadata for the current signing key.

## What It Does Not Decide

The application still owns its own identity logic:

- login endpoint
- password or external credential validation
- role and permission lookup
- refresh token behavior
- token revocation or blacklist behavior
- tenant-specific key storage or rotation

Those can be implemented by the application when needed. This package only prevents every small project from reimplementing the Asgard JWT shape by hand.

## Basic Usage

```csharp
var issuer = new AsgardJwtIssuer(new AsgardJwtSigningOptions
{
    Issuer = "https://auth.example.com",
    Audience = "asgard-api",
    KeyId = "main-key",
    RsaPrivateKeyPem = privateKeyPem,
    RsaPublicKeyPem = publicKeyPem
});

var token = issuer.Issue(new AsgardJwtSubject
{
    Subject = user.Id,
    UserId = user.Id,
    TenantId = tenantId,
    Roles = roles,
    Permissions = permissions,
    Scope = ["api"],
    UserMetadatas = userMetadatas,
    TenantMetadata = tenantMetadata
});
```

## Backend Service Token

```csharp
var token = issuer.Issue(new AsgardJwtSubject
{
    Subject = "orders-worker",
    ClientId = "orders-worker",
    TokenType = AsgardJwtConstants.BackendServiceTokenType,
    Scope = ["jobs.execute"]
});
```

Backend service tokens must include `client_id` and must not include `user_id`.

## Related Package

Use `Asgard.Heimdall.JwtSigning.AspNetCore` when you also want ASP.NET Core discovery and JWKS endpoints.
