---
name: identity-integration
description: Asgard 身份集成 skill。Use when designing or integrating login flows, IDP/OIDC wiring, Web SPA PKCE, backend JWT Bearer validation, token claim contracts, `/userinfo` boundaries, or deciding how frontend, IDP, gateway, and Asgard APIs should cooperate.
---

# Identity Integration

## 作用

本 skill 负责“身份系统怎么接起来”，也就是把前端、IDP、网关、Asgard API、claim 契约、授权链路放到同一张图里解释清楚。

它回答的问题不是“某个 claim 字段叫什么”，而是：

- Web 前端应该走什么登录流
- Asgard API 应该如何验证 token
- IDP 应该把哪些 claim 放进 access token
- OIDC `/userinfo` 能做什么，不能做什么
- 前端、后端、网关之间哪些职责该分开

## 什么时候使用

- 设计或评审登录接入方案时
- Web 前端要接 OIDC / OAuth 2.1 登录时
- 需要判断 SPA 是否应该使用 Authorization Code + PKCE 时
- 宿主要配置 `host.auth`、`JwtBearer`、OIDC discovery、JWKS 校验时
- 需要对齐 IDP 应输出哪些标准 claim 才能完成 Asgard 登录集成时
- 需要区分“认证集成”“身份模型”“授权 DSL”“密码哈希/加密”各自边界时

## 先记住的职责边界

| 主题 | 应该使用哪个 skill |
|------|--------------------|
| Web / IDP / token / 登录流接线 | `$identity-integration` |
| `AbsAsgardUserInfo`、标准 claim 字段、身份快照 | `$asgard-identity-userinfo` |
| Application Manifest、Tenant 绑定、应用管理员授权与版本 | `$heimdall-application-rbac` |
| `AsgardAuth*`、授权 DSL、`token_type` 授权表达式 | `$asgard-auth-authorization` |
| 密码哈希、数据加密、密钥生成 | `$asgard-security` |
| 宿主 `host.auth` / 中间件接线 | `$asgard-host-features` |
| Asgard 仓库级 C#/.NET 编码规范 | `$asgard-dotnet-10-csharp-14` |

## 集成决策

### Web 前端默认选型

如果是浏览器里的 Web SPA、管理后台、前端站点，对接外部或内部 IDP 时，默认使用：

- `Authorization Code Flow`
- `PKCE`
- 前端通过浏览器跳转到授权端点完成登录
- 前端拿到 `authorization code` 后，再向 IDP 换取 token

### 为什么默认是 PKCE

- 浏览器前端属于 public client，不能安全保存 client secret
- PKCE 可以防止授权码被截获后直接兑换 token
- 这是现代 Web 前端对接 OIDC / OAuth 的默认安全基线

### 不要默认这样做

- ❌ 不要让 Web SPA 使用 implicit flow
- ❌ 不要把前端当 confidential client 并硬塞 client secret
- ❌ 不要让前端自己伪造或约定一套与 Asgard 不兼容的 claim 名
- ❌ 不要把 `/userinfo` 当完整业务身份快照接口

## 推荐拓扑

### Web SPA + IDP + Asgard API

1. 前端把用户重定向到 IDP 授权端点
2. 前端使用 `code_challenge` / `code_verifier` 完成 PKCE
3. 前端拿 `authorization code` 去 token endpoint 换取 access token
4. 前端调用 Asgard API 时把 access token 放到 `Authorization: Bearer ...`
5. Asgard 宿主或 API 使用 OIDC discovery / JWKS 校验 access token
6. Asgard 身份层把 claim 解析成 `AbsAsgardUserInfo` / `AsgardIdentitySnapshot`
7. 授权层再由 `AsgardAuth*` 或 `AsgardAuthMatch(...)` 判定是否允许访问

### 后端服务调用 Asgard API

如果是服务对服务调用，不是浏览器用户登录：

- 不走 PKCE
- 使用明确的 machine-to-machine / backend-service 令牌方案
- 令牌里必须遵守 Asgard 的 `token_type=BackendService` 契约
- 需要 `client_id`，不要伪装成用户登录令牌

## Asgard Web 接入硬约束

### 前端

- Web 前端默认走 `Authorization Code + PKCE`
- 前端只负责发起登录、保存会话状态、携带 access token 调 API
- 前端不要假设自己能从 `/userinfo` 还原完整 `AbsAsgardUserInfo`

### IDP

- IDP 负责签发 access token
- IDP 应输出 Asgard 约定的标准 claim
- 用户令牌与后端服务令牌必须能通过 `token_type` 明确区分

### Asgard API

- Asgard API 负责校验 token 的签名、颁发者、受众、生命周期
- API 负责把标准 claim 还原为 Asgard 身份快照
- API 不负责替前端完成 OIDC 登录页面流程

## Claim 契约总览

Asgard 运行时真正关心的是这组标准字段：

- `sub`
- `user_id`
- `tenant_id`
- `client_id`
- `token_type`
- `roles`
- `permissions`
- `scope`
- `userMetadatas`
- `tenantMetadata`

字段语义、编码格式、扩展方式请继续读 `$asgard-identity-userinfo`。

这里先记住两个结论：

- `roles`、`permissions`、`scope` 约定为 JSON 数组字符串
- `userMetadatas`、`tenantMetadata` 约定为 JSON 对象字符串

## `userinfo` 的边界

OIDC 标准 `userinfo` 适合补充通用用户资料，例如：

- `sub`
- `name`
- `email`
- `phone_number`

但它**不是** Asgard 运行时的完整身份快照接口。

所以：

- 不要假设 `/userinfo` 一定返回 `user_id`、`roles`、`permissions`、`scope`
- 需要还原 Asgard 身份时，优先基于 access token / claims
- 如果业务系统需要完整“当前用户详情”接口，应单独设计业务 API

## 后端校验模式

### 通用 ASP.NET Core / Asgard API

后端资源服务器通常遵循这个模式：

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "{Authority}";
        options.Audience = "{Audience}";
        options.RequireHttpsMetadata = true;
    });
```

### Asgard 宿主

如果走 Yggdrasil / `host.auth` 默认链路：

- 宿主可以代管默认 JWT Bearer 注册
- 宿主会在认证后参与补充 `tenant_id`
- 授权链路与 `AsgardAuth` 仍由 Asgard 统一接管

详细配置和中间件顺序请继续读 `$asgard-host-features`。

## Web SPA 推荐检查清单

- 使用 OIDC Authorization Code Flow
- 开启 PKCE
- 使用受信任的授权服务器地址
- 使用 HTTPS 回调地址
- 明确区分 `access token`、`id token`、`refresh token`
- 前端只把 `access token` 发送给 API
- API 只信任自己校验通过的 Bearer token
- 不要把 UI 展示字段和授权字段混为一谈

## 典型反模式

- ❌ 在 `$asgard-dotnet-10-csharp-14` 里讨论前端 Web 登录流
- ❌ 在 `$asgard-security` 里讨论 PKCE、OIDC 登录页面跳转
- ❌ 在 `$asgard-identity-userinfo` 里直接展开整套前端授权码流程
- ❌ 只设计前端登录成功页面，不定义后端 token 校验方式
- ❌ 只定义 JwtBearer 校验，不定义 IDP claim 契约
- ❌ 把用户登录令牌和后端服务令牌混成同一套语义

## 推荐协同 skill

- 需要实现 Heimdall BackendService 只读目录、身份失效 Webhook 或身份对账：`$heimdall-service-integration`
- 需要设计 Heimdall 应用权限模板、Tenant 绑定和应用管理员授权范围：`$heimdall-application-rbac`
- 需要设计 Asgard 标准 claim、`AbsAsgardUserInfo`、测试身份构造：`$asgard-identity-userinfo`
- 需要编写授权规则或调试 `AsgardAuthMatch(...)`：`$asgard-auth-authorization`
- 需要配置宿主认证、CORS、Swagger、中间件顺序：`$asgard-host-features`
- 需要写 Asgard C# 实现代码和测试：`$asgard-dotnet-10-csharp-14`

## 参考资料

需要核对 Asgard 身份模型时，优先读 `references/`：

- `AbsAsgardUserInfo.cs`
- `AsgardClaimTypes.cs`
- `AsgardIdentitySnapshot.cs`
- `AsgardTokenProfiles.cs`
- `DefaultAsgardIdentityContextResolver.cs`
- `IAsgardIdentityContext.cs`

代码范本请看 `templates/`：

- `BackendJwtBearer.cs.template`
- `OidcPkceChecklist.md.template`
- `TokenContract.json.template`
