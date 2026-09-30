## 签发端接入步骤

### 1. 注册服务

在签发端宿主中注册 `AddAsgardHeimdallJwtSigning`。优先用直接、可读、少配置的写法：

```csharp
builder.Services.AddAsgardHeimdallJwtSigning(options =>
{
    options.Issuer = "https://auth.example.com/scm";
    options.Audience = "scm-api";
    options.DiscoveryPathPrefix = "/scm";
    options.KeyId = "scm-main";
    options.RsaPrivateKeyPem = privateKeyPem;
    options.AccessTokenLifetime = TimeSpan.FromHours(1);
});
```

必要配置：

- `Issuer`：签发者地址，必须与资源服务 `issuerTemplate` 对齐
- `Audience`：默认受众，必须与资源服务 `audience` 对齐
- `KeyId`：当前签名密钥 ID，会进入 JWT header 和 JWKS
- `RsaPrivateKeyPem` 或 `SymmetricSecurityKey`：二选一

常用但仍然简单的配置：

- `DiscoveryPathPrefix`：discovery/JWKS 的路径前缀，例如 `/scm`
- `RsaPublicKeyPem`：可显式提供公钥；不提供时由私钥导出

高级逃生口：

- `JwksUriOverride`：只有反向代理或网关改写导致外部 JWKS 地址不同于 issuer 派生地址时才用

默认算法是 `RS256`。生产环境优先使用 RSA 私钥签发、公钥验证。

### 2. 映射 discovery/JWKS

在 endpoint 映射阶段加入：

```csharp
app.MapAsgardHeimdallJwtSigningDiscovery();
```

它会暴露：

```text
/.well-known/openid-configuration
/.well-known/jwks.json
```

如果配置：

```csharp
options.Issuer = "https://auth.example.com/scm";
options.DiscoveryPathPrefix = "/scm";
```

它会暴露：

```text
/scm/.well-known/openid-configuration
/scm/.well-known/jwks.json
```

discovery 文档应保持以下关系：

```text
issuer == options.Issuer.TrimEnd('/')
jwks_uri == options.Issuer.TrimEnd('/') + "/.well-known/jwks.json"
```

只有当网关、反向代理、内外网地址导致 JWKS 外部可访问地址不同于 issuer 派生地址时，才使用 `JwksUriOverride`。

资源服务通过 discovery 找到 JWKS，再用公钥校验 JWT 签名。

不要在业务项目里自己手写 `/.well-known/openid-configuration` 或 `/.well-known/jwks.json`。如果已经引用 `Asgard.Heimdall.JwtSigning.AspNetCore`，这些端点就应由包提供；业务项目只负责调用映射扩展。

### 3. 自己实现登录 API

mini issuer 不校验账号密码。登录 API 应先完成业务自己的校验，再调用 `IAsgardJwtIssuer.Issue(...)`。

```csharp
app.MapPost("/login", async (LoginRequest request, IAsgardJwtIssuer issuer, IUserLoginService loginService) =>
{
    var loginUser = await loginService.ValidateAsync(request.UserName, request.Password);
    if (loginUser is null)
    {
        return Results.Unauthorized();
    }

    var token = issuer.Issue(new AsgardJwtSubject
    {
        Subject = loginUser.Subject,
        UserId = loginUser.UserId,
        TenantId = loginUser.TenantId,
        Roles = loginUser.Roles,
        Permissions = loginUser.Permissions,
        Scope = ["api"],
        Name = loginUser.DisplayName,
        Email = loginUser.Email,
        AuthenticationTime = DateTimeOffset.UtcNow,
        SessionId = loginUser.SessionId
    });

    return Results.Ok(token);
});
```

返回对象 `AsgardJwtIssueResult` 包含：

- `AccessToken`
- `TokenType`，固定为 HTTP Bearer 类型
- `ExpiresIn`
- `IssuedAt`
- `ExpiresAt`
- `Jti`

登录接口不要在每次请求里 `new AsgardJwtIssuer(...)`，也不要为了不同 tenant 动态拼 issuer。签发器应由 DI 注册，登录逻辑只注入 `IAsgardJwtIssuer`。

### 小固定项目写法

如果项目本身就是固定 SCM、小后台、单租户或租户只是历史字段，直接把项目事实写死：

```csharp
var token = issuer.Issue(new AsgardJwtSubject
{
    Subject = user.Id,
    UserId = user.Id,
    TenantId = "scm",
    Roles = ["scm-user"],
    Permissions = ["scm.api"],
    Scope = ["api"],
    Name = user.DisplayName,
    AuthenticationTime = DateTimeOffset.UtcNow
});
```

这种场景不要新增 `DefaultTenantId`、`IssuerTemplate`、`/tenants/{tenant}` discovery 路由、tenant 级 JWKS、tenant 级 key provider。那些是完整身份中心或真正多租户认证系统的复杂度，不是 mini issuer 的复杂度。
