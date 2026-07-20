---
name: asgard-mini-jwt-issuer
description: Asgard mini 认证颁发 / Heimdall lightweight JWT issuer skill。Use when a small Asgard project needs to issue Asgard-compatible JWT access tokens without the full Heimdall IDP, wire `Asgard.Heimdall.JwtSigning`, expose OIDC discovery/JWKS endpoints, create user-login or backend-service tokens, configure resource services with `host.auth.jwt`, or explain the mini issuer boundary to other teams.
---

# Asgard Mini JWT Issuer

## 作用

本 skill 说明 Heimdall 的 mini 认证颁发方案：用 `Asgard.Heimdall.JwtSigning` / `Asgard.Heimdall.JwtSigning.AspNetCore` 在小项目里快速签发 Asgard 可识别的 JWT access token。

核心目标是快捷、方便、少理解协议：业务项目已经有自己的登录校验，登录成功后只把“用户是谁、租户是谁、有什么角色权限”交给包；包自动完成 Asgard claim、JWT 签发、discovery、JWKS。

一句话：**业务只写登录，包负责发 token 和暴露验签元数据。**

## 最快接入

默认按这个最短路径实现，不要一上来设计额外封装：

```csharp
builder.Services.AddAsgardHeimdallJwtSigning(options =>
{
    options.Issuer = "https://auth.example.com/scm";
    options.Audience = "scm-api";
    options.DiscoveryPathPrefix = "/scm";
    options.KeyId = "scm-main";
    options.RsaPrivateKeyPem = privateKeyPem;
});

app.MapAsgardHeimdallJwtSigningDiscovery();
```

登录成功后只调用一次 `Issue(...)`：

```csharp
var token = issuer.Issue(new AsgardJwtSubject
{
    Subject = user.Id,
    UserId = user.Id,
    TenantId = "scm",
    Roles = ["scm-user"],
    Permissions = ["scm.api"],
    Scope = ["api"]
});
```

配置 `DiscoveryPathPrefix = "/scm"` 后，包自动暴露：

```text
/scm/.well-known/openid-configuration
/scm/.well-known/jwks.json
```

业务方不要手写 discovery/JWKS，不要自己拼 `jwks_uri`，不要新增 mini issuer 包外的一层复杂胶水。

## mini 的第一原则

mini issuer 的价值是快捷、少写、少懂、少配置。实现时优先选择最朴素的形态：

- 直接调用 `AddAsgardHeimdallJwtSigning(...)`
- 直接调用 `app.MapAsgardHeimdallJwtSigningDiscovery()`
- 登录接口只做业务校验，然后注入 `IAsgardJwtIssuer` 调 `Issue(...)`
- 固定小项目里的项目事实，例如 `TenantId = "scm"`、`Audience = "scm-api"`、`Roles = ["scm-user"]`
- 业务系统有路径前缀时只配置 `DiscoveryPathPrefix = "/scm"`，不要自己手写 discovery/JWKS
- 只配置环境会变的东西，例如正式 issuer 域名、RSA 私钥、公钥、token 有效期

不要把“未来可能变化”提前做成配置。对小项目来说，固定事实 hardcode 比到处配置更清楚，也更符合 mini 的目的。看到复杂抽象时，优先把代码收回到上面的最快接入形态。

## 先记住边界

mini issuer 只负责这些事：

- 按 Asgard 标准 claim 契约签发 access token
- 支持用户登录令牌 `token_type=UserLogin`
- 支持后端服务令牌 `token_type=BackendService`
- 支持 RSA 签名和对称签名
- 暴露 `/.well-known/openid-configuration`
- 暴露 `/.well-known/jwks.json`
- 支持通过 `DiscoveryPathPrefix` 暴露 `/{prefix}/.well-known/openid-configuration` 和 `/{prefix}/.well-known/jwks.json`
- 保证 token `iss`、discovery `issuer`、discovery `jwks_uri` 使用同一 issuer 语义
- 让资源服务继续使用 Asgard `host.auth.jwt` 校验 token

mini issuer 不负责这些事：

- 登录页、密码校验、短信验证码、外部账号绑定
- 角色/权限/租户从数据库怎么查
- refresh token、静默续期、撤销、黑名单
- 用户同意页、授权码模式、PKCE、完整 OIDC Provider
- 多租户密钥轮换、长期审计、风控策略
- Application Manifest、Tenant 绑定、角色同步或授权版本的权威计算

如果项目需要完整 Web 登录、Authorization Code + PKCE、refresh token、同意页、设备码等完整 IDP 能力，使用完整 Heimdall / OIDC 方案，不要把 mini issuer 硬扩成完整身份中心。

## 什么时候选 mini issuer

优先选 mini issuer：

- 小型 Asgard 后端项目已有自己的账号校验逻辑
- 内部系统只需要登录成功后发一个 Bearer token 调 API
- 项目不需要完整 OIDC 授权码流程
- 其它 Asgard 资源服务需要通过 discovery + JWKS 校验这个 token
- 后台任务、内部 worker 需要拿一个 `BackendService` token 调 Asgard API

不要选 mini issuer：

- 浏览器 SPA 要接标准 OAuth/OIDC 登录
- 要给第三方应用开放授权码、scope consent、refresh token
- 要做统一账号中心和跨系统 SSO
- 要支持复杂租户级密钥管理、吊销、会话治理

## 包与职责

| 包 | 用途 |
|----|------|
| `Asgard.Heimdall.JwtSigning` | 核心 JWT 签发器，提供 `IAsgardJwtIssuer`、`AsgardJwtIssuer`、`AsgardJwtSubject` |
| `Asgard.Heimdall.JwtSigning.AspNetCore` | ASP.NET Core 集成，注册签发器并映射 discovery/JWKS 端点 |

签发端应用引用这两个包。资源服务通常不需要引用它们，只配置 `host.auth.jwt` 指向签发端 issuer。

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

## 用户令牌 claim 规则

用户登录令牌默认 `token_type=UserLogin`。最小可用主体：

```csharp
var token = issuer.Issue(new AsgardJwtSubject
{
    Subject = user.Id,
    UserId = user.Id,
    TenantId = tenantId,
    Roles = ["user"],
    Permissions = ["profile.read"],
    Scope = ["api"]
});
```

必须提供：

- `Subject`

强烈建议提供：

- `UserId`
- `TenantId`
- `Roles`
- `Permissions`
- `Scope`

可选提供：

- `Name`
- `Email`
- `PhoneNumber`
- `AuthenticationTime`
- `SessionId`
- `UserMetadatas`
- `TenantMetadata`

应用域 Token 还可提供：

- `ApplicationId`
- `ApplicationManifestVersion`
- `ApplicationAuthorizationVersion`
- `TenantAuthorizationVersion`

这些字段必须来自权威授权快照。mini issuer 只负责原样签发，不能自行查询、推导或递增版本；需要设计其来源时使用 `$heimdall-application-rbac`。

集合字段由签发器自动序列化为 JSON 数组字符串；字典字段自动序列化为 JSON 对象字符串。不要在调用前手动拼逗号字符串。

## 后端服务 token

后端服务调用 Asgard API 时使用 `BackendService` token：

```csharp
var token = issuer.Issue(new AsgardJwtSubject
{
    Subject = "orders-worker",
    ClientId = "orders-worker",
    TokenType = AsgardJwtConstants.BackendServiceTokenType,
    Scope = ["jobs.execute"]
});
```

硬约束：

- 必须提供 `Subject`
- 必须提供 `ClientId`
- 必须设置 `TokenType = AsgardJwtConstants.BackendServiceTokenType`
- 不能提供 `UserId`

资源服务可以用授权表达式限制后端服务 token：

```csharp
[AsgardAuthMatch("token_type = 'BackendService' and scope = 'jobs.execute'")]
```

## 基于 `AbsAsgardUserInfo` 签发

如果项目已经构造了 Asgard 用户信息对象，可以直接签发：

```csharp
var token = issuer.Issue(userInfo);
```

签发器会读取 `AbsAsgardUserInfo` 的标准字段并转成 `AsgardJwtSubject`。这比手动拼 claim 更稳，尤其适合已有 Asgard 身份上下文、测试身份或自定义 `AbsAsgardUserInfo` 子类的项目。

## 单次签发覆盖选项

`AsgardJwtIssueOptions` 可覆盖单次签发：

```csharp
var token = issuer.Issue(subject, new AsgardJwtIssueOptions
{
    Audience = "asgard-admin-api",
    Lifetime = TimeSpan.FromMinutes(30),
    IssuedAt = DateTimeOffset.UtcNow,
    Jti = Guid.NewGuid().ToString("N")
});
```

常见用途：

- 给不同资源服务发不同 audience
- 给高风险入口缩短 token 生命周期
- 测试中固定 `IssuedAt` / `Jti`

## 资源服务配置

资源服务继续使用 Asgard 宿主 JWT 配置，不需要引用 mini issuer 包：

```yaml
host:
  auth:
    enabled: true
    jwt:
      issuerTemplate: "https://auth.example.com/scm"
      audience: "scm-api"
      requireHttpsMetadata: true
      discoveryCacheMinutes: 60
      jwksCacheMinutes: 60
```

对齐规则：

- `issuerTemplate` 必须等于签发端 `Issuer`
- `audience` 必须等于签发端 `Audience` 或单次签发覆盖的 audience
- 资源服务必须能访问签发端 discovery 和 JWKS；如果配置 `DiscoveryPathPrefix = "/scm"`，就是 `/scm/.well-known/openid-configuration` 和 `/scm/.well-known/jwks.json`
- 生产环境保持 `requireHttpsMetadata: true`

如果是本地开发，可以临时使用 HTTP issuer，但不要把开发配置带到生产。

## 密钥建议

优先使用 RSA：

- 私钥只放签发端
- 公钥可通过 JWKS 暴露给资源服务
- `KeyId` 要稳定，换密钥时同步更新
- 私钥不要写死在代码或仓库里，通过安全配置、环境变量、密钥服务注入

对称密钥只适合非常小的受控场景：

- 使用 `SymmetricSecurityKey`
- 算法要与密钥类型匹配
- 所有验证方共享同一密钥，泄露影响更大

## 排查清单

资源服务返回 401 时先查：

- 签发端 discovery 是否可访问
- JWKS 中是否有当前 `kid`
- token header 的 `kid` 是否等于 `options.KeyId`
- token 的 `iss` 是否等于资源服务 `issuerTemplate`
- discovery 的 `issuer` 是否等于 token 的 `iss`
- discovery 的 `jwks_uri` 是否等于 `Issuer + "/.well-known/jwks.json"`，或显式配置的 `JwksUriOverride`
- token 的 `aud` 是否等于资源服务 `audience`
- 资源服务机器时间是否明显偏差
- token 是否过期
- token 是否用资源服务不支持的算法签发

授权返回 403 时先查：

- `token_type` 是否正确
- 用户令牌是否有需要的 `roles` / `permissions`
- 后端服务令牌是否有需要的 `scope`
- `roles`、`permissions`、`scope` 是否被签发器正常写成 JSON 数组 claim
- `tenant_id` 是否符合资源服务租户隔离要求

## 与其它 skill 的边界

| 问题 | 使用 |
|------|------|
| mini JWT 颁发、JWKS/discovery、签发端/资源端对接 | `$asgard-mini-jwt-issuer` |
| 完整 Web 登录、PKCE、IDP/API/SPA 集成关系 | `$identity-integration` |
| claim 字段、`AbsAsgardUserInfo`、身份快照 | `$asgard-identity-userinfo` |
| Application Manifest、Tenant 绑定、管理员 Grant 与授权版本 | `$heimdall-application-rbac` |
| `host.auth.jwt`、中间件顺序、宿主配置 | `$asgard-host-features` |
| `AsgardAuthMatch`、角色权限和 `token_type` 授权 | `$asgard-auth-authorization` |
| C# 代码规范和测试写法 | `$asgard-dotnet-10-csharp-14` |

## 模板

可复制模板见 `assets/`：

- `MinimalIssuerProgram.cs.template` - 最小签发端 Program 示例
- `LoginEndpoint.cs.template` - 登录成功后签发用户 token
- `BackendServiceToken.cs.template` - 后端服务 token 签发
- `ResourceServiceAuth.app.yaml.template` - 资源服务 JWT 校验配置

## 参考源码

需要核对 mini issuer 当前实现时读 `references/`：

- `AsgardJwtSigning.Readme.md`
- `AsgardJwtSigning.AspNetCore.Readme.md`
- `AsgardJwtIssuer.cs`
- `AsgardJwtSubject.cs`
- `AsgardJwtSigningOptions.cs`
- `AsgardHeimdallJwtSigningServiceCollectionExtensions.cs`
- `AsgardHeimdallJwtSigningEndpointRouteBuilderExtensions.cs`

## 不要这样做

- 不要把 mini issuer 当完整 OIDC Provider
- 不要自己手写 discovery/JWKS controller 或 endpoint；使用 `MapAsgardHeimdallJwtSigningDiscovery()`
- 不要开放两个任意 discovery/JWKS 路径配置；业务前缀用 `DiscoveryPathPrefix`
- 不要为了小固定项目设计 `/tenants/{tenant}/.well-known/...` 这类 tenant 级 issuer 路由
- 不要把固定项目事实做成配置项，例如固定 SCM 项目的 `tenant_id`、默认角色、默认权限、默认 scope
- 不要在登录时根据请求 tenant 动态 `new AsgardJwtIssuer(...)`
- 不要把资源服务的 `issuerTemplate` 能力误读成签发端必须实现多 tenant issuer 模板
- 不要在登录接口里跳过真实账号校验直接发 token
- 不要手写 Asgard claim JSON 字符串绕过 `AsgardJwtSubject`
- 不要给 `BackendService` token 塞 `UserId`
- 不要把 RSA 私钥提交到仓库
- 不要让资源服务引用签发包后自己本地验私钥，资源服务应走 discovery/JWKS
- 不要让签发端 `Issuer` 和资源端 `issuerTemplate` 各写一套不一致的地址

## 反面教材：过度工程化的 mini issuer

看到下面形态时，应立即收敛回官方集成方式：

- 业务项目有自定义 `AuthEndpointExtensions` 同时手写 login、discovery、JWKS
- 业务项目有 `IssuerTemplate = ".../tenants/{tenant}"`，登录时替换 tenant 生成 issuer
- 登录请求携带 `TenantId` 只是为了影响 issuer 或 discovery 地址
- 有 `DefaultTenantId`、tenant 级 key provider、tenant 级 JWKS，但业务明确是固定小项目
- 每次登录都构造新的 `AsgardJwtSigningOptions` 和 `AsgardJwtIssuer`

正确修正方向：

- 保留普通登录 API 或 controller
- 使用 DI 中的 `IAsgardJwtIssuer`
- 使用 `MapAsgardHeimdallJwtSigningDiscovery()`
- 固定 `TenantId = "scm"` 或项目约定值
- 配置只保留真正跨环境变化的 issuer、密钥和过期时间
