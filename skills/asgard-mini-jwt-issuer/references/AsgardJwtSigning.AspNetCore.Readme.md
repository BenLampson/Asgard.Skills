# Asgard.Heimdall.JwtSigning.AspNetCore

ASP.NET Core integration for `Asgard.Heimdall.JwtSigning`.

This package exposes discovery and JWKS endpoints so Asgard resource services can validate JWTs through the existing `host.auth.jwt` flow.

## Endpoints

```text
/.well-known/openid-configuration
/.well-known/jwks.json
```

## Usage

```csharp
builder.Services.AddAsgardHeimdallJwtSigning(options =>
{
    options.Issuer = "https://auth.example.com";
    options.Audience = "asgard-api";
    options.KeyId = "main-key";
    options.RsaPrivateKeyPem = privateKeyPem;
    options.RsaPublicKeyPem = publicKeyPem;
});

app.MapAsgardHeimdallJwtSigningDiscovery();
```

Your application still implements its own login API. Inside that API, inject `IAsgardJwtIssuer` and issue a token after your own user validation succeeds.

```csharp
app.MapPost("/login", (IAsgardJwtIssuer issuer) =>
{
    var token = issuer.Issue(new AsgardJwtSubject
    {
        Subject = "user-1",
        UserId = "user-1",
        TenantId = "11111111-2222-3333-4444-555555555555",
        Roles = ["user"],
        Permissions = ["profile.read"],
        Scope = ["api"]
    });

    return Results.Ok(token);
});
```

## Resource Service Configuration

Asgard resource services do not need to reference this package. They keep using Asgard's built-in JWT authentication and point `issuerTemplate` to the issuer hosted by this package.

```yaml
host:
  auth:
    enabled: true
    jwt:
      issuerTemplate: "https://auth.example.com"
      audience: "asgard-api"
```
