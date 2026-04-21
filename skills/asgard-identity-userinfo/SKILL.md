---
name: asgard-identity-userinfo
description: Asgard 身份用户信息 skill。Use when a task needs AbsAsgardUserInfo, IAsgardIdentityContext, standard claim contract mapping, tenant/user identity modeling, identity snapshot structure, or test/user-session construction in Asgard.
---

# Asgard Identity UserInfo

## 作用

本 skill 专门说明 Asgard 的统一用户信息模型，核心对象是 `AbsAsgardUserInfo`。

它解决的问题不是“怎么随便塞一点用户字段进去”，而是：

- IDP 应该产出什么 claim
- 业务代码应该从哪里取用户信息
- 自定义用户信息类应该继承谁
- 测试环境应该如何构造与生产一致的身份数据
- 授权表达式里的 `role`、`permission`、`scope`、`userMetadata`、`tenantMetadata` 到底从哪里来
- JWT 里的 `token_type` 到底如何进入授权层

## 什么时候使用

- 需要定义或调整 Asgard 标准 claim 契约时
- 需要定义“当前用户信息”模型时
- 要从 `AbsAsgardContext.IdentityContext` 读取当前用户信息时
- 编写授权、租户隔离、权限判断逻辑时
- 编写需要模拟登录态的测试时

如果问题重点是“前端 Web 如何登录”“为什么 Web 要走 PKCE”“IDP、前端、Asgard API 之间怎么接”，优先切到 `$identity-integration`；本 skill 主要负责 claim 契约和身份模型本身。

## 先记住的硬约束

| 规则 | 要求 |
|------|------|
| **统一基类** | 所有 Asgard 用户信息模型都必须以 `AbsAsgardUserInfo` 为基类，不要自己另起一套“用户上下文 DTO” |
| **统一入口** | 运行时读取当前用户信息时，优先从 `IAsgardIdentityContext` / `AbsAsgardContext.IdentityContext` 获取 |
| **统一 claim 名** | 框架内置识别的 claim 是 `sub`、`user_id`、`tenant_id`、`client_id`、`token_type`、`roles`、`permissions`、`scope`、`userMetadatas`、`tenantMetadata` |
| **集合编码** | `roles`、`permissions`、`scope` 必须是 JSON 数组字符串，不是逗号拼接字符串 |
| **字典编码** | `userMetadatas`、`tenantMetadata` 必须是 JSON 对象字符串 |
| **租户判定** | 默认解析器会把可解析的 `tenant_id` 视为租户用户；没有合法租户 ID 时会落到 `UserType.Platform` |
| **扩展方式** | 需要额外字段时，继承 `AbsAsgardUserInfo` 并重写 `InitFromClaims()` / `ToClaims()`，不要绕过基类 |
| **测试一致性** | 测试造登录态时也必须按这套 claim 契约构造，不能只塞 `ClaimTypes.NameIdentifier` 然后假设框架能自动懂 |

## `AbsAsgardUserInfo` 标准字段

| 字段 | 含义 | 说明 |
|------|------|------|
| `Sub` | 用户主体标识 | 必填，`ToClaims()` 一定会输出 `sub` |
| `UserId` | 业务用户 ID | 可选，映射 `user_id` |
| `TenantId` | 租户 ID | 可选，映射 `tenant_id` |
| `ClientId` | 后端服务调用方 ID | 可选，映射 `client_id` |
| `Roles` | 角色列表 | 映射 `roles`，JSON 数组 |
| `Permissions` | 权限列表 | 映射 `permissions`，JSON 数组 |
| `Scope` | 作用域列表 | 映射 `scope`，JSON 数组 |
| `UserMetadatas` | 用户元数据 | 映射 `userMetadatas`，JSON 对象 |
| `TenantMetadata` | 租户元数据 | 映射 `tenantMetadata`，JSON 对象 |

## 框架默认行为

### 解析行为

- `DefaultAsgardIdentityContextResolver` 会从 `HttpContext.User.Claims` 创建 `DefaultAsgardUserInfo`
- 然后调用 `InitFromClaims(...)` 还原基础字段
- 如果 `TenantId` 能解析成 `Guid`，用户类型会被判定为 `UserType.Tenant`
- 如果 `TenantId` 为空或不是合法 `Guid`，默认会判定为 `UserType.Platform`
- `token_type` 只识别官方值 `UserLogin` / `BackendService`
- `client_id` 是后端服务令牌唯一认可的调用方标识

### 后端服务令牌约定

Asgard 现在正式收口了后端服务令牌的最小契约：

- 必填：`sub`、`client_id`、`token_type=BackendService`
- 可选：`tenant_id`、`scope`
- 禁止：`user_id`

如果认证后的后端服务令牌缺少 `client_id`，或者同时带了 `user_id`，框架会在认证阶段直接判定为不符合约定。

### 授权行为

Asgard 授权表达式、元数据匹配会直接读取 `AbsAsgardUserInfo` 中这些字段：

- `Roles`
- `Permissions`
- `Scope`
- `UserMetadatas`
- `TenantMetadata`

除此之外，`AsgardAuthMatch(...)` 现在还支持直接读取身份快照中的：

- `TokenType`，DSL 字段名为 `token_type`

这意味着 IDP 如果随意改字段名、改编码格式、改大小写，授权判断就会直接失效。

示例：

```csharp
[AsgardAuthMatch("token_type = 'BackendService' and scope = 'jobs.execute'")]
```

### `roles` / `permissions` 为空的语义

需要单独强调这个常见误区：

- `roles: []`、`permissions: []` 在协议层是合法的，不属于违规 claim
- 但这会导致大部分基于角色/权限的 `AsgardAuth` 表达式恒失败
- 此时通常只能依赖 `scope`、`userMetadata`、`tenantMetadata` 或 DSL 其他条件放行

建议：

- 不要把“空数组合法”误解成“可用于常规受保护接口”
- 若系统存在默认受保护接口，请为用户发放最小可用角色或权限集

### 最小可用 claims 集（建议模板）

以下模板可作为“可通过基础授权链路”的最小起点，按业务再增量扩展：

```json
{
  "sub": "user-sub-001",
  "user_id": "user-001",
  "tenant_id": "11111111-2222-3333-4444-555555555555",
  "token_type": "UserLogin",
  "roles": ["user"],
  "permissions": ["profile.read"],
  "scope": ["api"],
  "userMetadatas": {},
  "tenantMetadata": {}
}
```

后端服务令牌最小示例：

```json
{
  "sub": "svc-orders-job",
  "client_id": "orders-job-runner",
  "token_type": "BackendService",
  "scope": ["jobs.execute"]
}
```

## IDP 开发规范

### 应该怎么做

1. 登录成功后，先构建一个继承自 `AbsAsgardUserInfo` 的对象，或者直接使用 `DefaultAsgardUserInfo`
2. 把标准字段填满
3. 通过 `ToClaims()` 生成标准 claims
4. 显式补上 `token_type`
5. 如果有扩展字段，再追加自定义 claim，或者在子类重写 `ToClaims()` / `InitFromClaims()`

### 不应该怎么做

- 不要只往 token 里塞 `name`、`email`、`role` 之类零散字段，然后让业务层自己猜
- 不要把 `roles`、`permissions` 写成 `"admin,user"` 这种逗号字符串
- 不要把租户信息只放在自定义 claim 里，却不写 `tenant_id`
- 不要在不同 IDP、不同插件里各自发明 `userInfo` JSON 结构
- 不要在业务层重复解析 `ClaimsPrincipal`，导致每个项目都写一份“用户信息还原器”
- 不要输出任何非官方 claim 名，框架只认标准字段

## 关于 OIDC `userinfo` 的边界

很多团队在接入 IDP 时，容易把 OIDC 的 `/userinfo` 和 Asgard 业务运行时的完整身份快照混为一谈，这里必须分清。

标准 `userinfo` 一般只按 scope 返回这些字段：

- `sub`
- `name`
- `email`
- `phone_number`
- `tenant_id`

这意味着：

- `/userinfo` 目前**不能**视为“完整 `AbsAsgardUserInfo` 快照接口”
- 如果下游要还原完整 `AbsAsgardUserInfo`，应该优先基于 token / claims 本身，而不是假设 `/userinfo` 会把 `user_id`、`roles`、`permissions`、`scope`、`userMetadatas`、`tenantMetadata` 全部回出来
- 如果以后要提供“面向 Asgard 业务系统的完整用户信息接口”，应该单独设计，不要直接把标准 OIDC `userinfo` 语义搞混

## 自定义扩展模式

如果项目确实需要额外字段，例如：

- `Name`
- `DisplayName`
- `AvatarUrl`
- `DepartmentId`
- `EmployeeNumber`

请使用下面的模式：

```csharp
public class MyAsgardUserInfo : AbsAsgardUserInfo
{
    public string? Name { get; set; }

    public override void InitFromClaims(IEnumerable<Claim> claims)
    {
        base.InitFromClaims(claims);

        foreach (var claim in claims)
        {
            if (claim.Type == "name")
            {
                Name = claim.Value;
            }
        }
    }

    public override IEnumerable<Claim> ToClaims()
    {
        var claims = base.ToClaims().ToList();

        if (!string.IsNullOrWhiteSpace(Name))
        {
            claims.Add(new Claim("name", Name));
        }

        return claims;
    }
}
```

## 关于 `Name` 字段的额外约定

Asgard 授权计算器在做 `name` 相关匹配时，除了查 claim，还会尝试从 `AbsAsgardUserInfo` 的公开属性里反射读取 `Name`。

因此：

- 如果你的用户信息需要支持基于姓名 / 显示名的授权表达式匹配，建议显式提供 `Name` 属性
- 如果没有 `Name` 属性，框架会退回读取 `ClaimTypes.Name` 或 `"name"` claim

## 业务侧读取方式

### 正确方式

```csharp
public class CurrentUserService(AbsAsgardContext asgardContext)
{
    private readonly AbsAsgardContext _asgardContext = asgardContext;

    public string? GetCurrentUserId()
    {
        return _asgardContext.IdentityContext?.UserInfo?.UserId;
    }
}
```

### CRUD 审计字段的推荐写法

凡是业务实体里有这些字段：

- `CreateBy`
- `UpdateBy`
- `TenantId`

都应该优先从身份上下文填充，而不是留空：

```csharp
var userInfo = _asgardContext.IdentityContext?.UserInfo;
if (string.IsNullOrWhiteSpace(userInfo?.UserId))
{
    throw new UnauthorizedAccessException("当前登录信息无效，无法确定用户标识。");
}

entity.CreateBy = userInfo.UserId;
entity.UpdateBy = userInfo.UserId;
entity.TenantId = userInfo.TenantId;
```

如果接口允许匿名访问，那就必须显式设计匿名场景下的审计策略，不能默认漏掉这些字段。

### 不推荐方式

- 到处直接读 `HttpContext.User.Claims`
- 每个服务都重复写一份 claim 解析逻辑
- 业务层自己猜 `tenant_id`、`roles` 的格式

## 测试规范

测试里如果要伪造登录态，请构造与生产一致的 claim：

```csharp
var userInfo = new DefaultAsgardUserInfo
{
    Sub = "user-sub-001",
    UserId = "user-001",
    TenantId = tenantId.ToString(),
    Roles = ["admin"],
    Permissions = ["users.read", "users.write"],
    Scope = ["api"]
};

var claims = userInfo.ToClaims().ToList();
claims.Add(new Claim("name", "测试管理员"));
```

不要再只写：

```csharp
new Claim(ClaimTypes.NameIdentifier, userId)
new Claim(ClaimTypes.Role, "Admin")
```

这套写法无法自动填充 `AbsAsgardUserInfo` 的标准字段，也不能完整验证 Asgard 的身份与授权链路。

## 推荐协同 skill

- 需要读取运行时身份上下文时：使用 `$asgard-context-usage`
- 需要写 Controller、增删改查接口、统一响应和当前用户读取示例时：结合 `$asgard-api-development`
- 需要写 `AsgardAuth` 表达式、授权 DSL 或按 `token_type` 控制接口访问时：结合 `$asgard-auth-authorization`
- 需要理解基类和字段语义时：使用 `$asgard-base-types`
- 需要写 Asgard C# 代码和测试时：同时遵守 `$asgard-dotnet-10-csharp-14`
- 需要处理宿主认证、授权、中间件接入时：结合 `$asgard-host-features`

## 参考资料

完整源码拷贝请参考 `references/`：

- `AbsAsgardUserInfo.cs` - 用户信息统一基类
- `IAsgardIdentityContext.cs` - 当前身份上下文读取入口
- `AsgardIdentitySnapshot.cs` - 当前身份快照模型
- `DefaultAsgardUserInfo.cs` - 默认用户信息实现
- `DefaultAsgardIdentityContextResolver.cs` - 默认 claims 解析器

## 源码锚点

以下锚点用于快速核对“claim 语义 -> 身份快照 -> 授权执行”的链路：

- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - 角色/权限/元数据授权特性
- `Common/Asgard.AspNetCore.Core/ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 注册授权处理器与策略
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Configurator.cs` - 默认 `UseAuthorization()` 接线
- `Common/Asgard.Abstractions.AspNetCore/Host/AuthOptions.cs` - 宿主认证开关边界语义

代码范本请参考 `templates/`：

- `CustomAsgardUserInfo.cs.template` - 自定义用户信息扩展模板
- `BuildIdentityClaims.cs.template` - IDP / 测试构造标准 claims 模板
- `BuildBackendServiceClaims.cs.template` - IDP / 测试构造后端服务 claims 模板

## 不要这样做

- ❌ 不要在不同插件或不同项目里各自定义一套“当前用户对象”
- ❌ 不要把 `AbsAsgardUserInfo` 当成随便塞字段的袋子，却不维护 `InitFromClaims()` / `ToClaims()` 对称性
- ❌ 不要让 IDP 输出一份 JSON 字符串 claim 再让业务代码自己反序列化整个用户对象
- ❌ 不要省略 `tenant_id` 却期待框架自动识别租户用户
- ❌ 不要把 `roles`、`permissions`、`scope` 写成逗号拼接字符串
- ❌ 不要在测试里使用与生产不一致的 claims 结构
