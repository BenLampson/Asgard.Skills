---
name: asgard-auth-authorization
description: Asgard 授权与 AsgardAuth skill。Use when designing or debugging AsgardAuth attributes, authorization DSL expressions, role/permission/scope checks, token_type-based access rules, or understanding how identity snapshot fields participate in authorization.
---

# Asgard Auth Authorization

## 作用

本 skill 专门说明 Asgard 的授权声明层，也就是 `AsgardAuth*` 特性与 `AsgardAuthMatch(...)` DSL。

它解决的问题包括：

- `AsgardAuth*` 到底支持哪些字段
- 角色 / 权限 / scope / metadata 应该怎么写
- JWT 里的 `token_type` 能不能直接参与授权
- 什么场景该用声明式授权，什么场景必须在业务层继续做资源边界校验
- 扩展 DSL 字段时应该改哪些源码点和测试点

## 什么时候使用

- 新增或修改 `[AsgardAuth*]` 授权声明时
- 编写 `AsgardAuthMatch("...")` 表达式时
- 需要按 `token_type`、角色、权限或 metadata 限制接口访问时
- 排查“JWT 明明有 claim，但 AsgardAuth 匹配不到”这类问题时
- 扩展 AsgardAuth DSL 支持新字段时

## 先记住的硬约束

| 规则 | 要求 |
|------|------|
| **职责边界** | `AsgardAuth` 只负责声明式资格判断，不替代业务资源归属校验 |
| **统一策略** | 所有 `AsgardAuth*` 特性统一挂到 `AsgardAuth` policy |
| **字段白名单** | DSL 只能访问框架显式支持的字段，不能假设 JWT 任意 claim 都可直接检索 |
| **身份来源** | 授权求值统一读取 `AsgardIdentitySnapshot` 与 `AbsAsgardUserInfo`，不要在授权 DSL 里假设能直接遍历原始 claims |
| **扩展闭环** | 扩字段时必须同时修改字段枚举、解析器、求值器和测试 |

## 当前支持的 DSL 字段

| 字段 | 来源 | 示例 |
|------|------|------|
| `role` | `UserInfo.Roles` | `role = 'admin'` |
| `permission` | `UserInfo.Permissions` | `permission in ('orders.read', 'orders.write')` |
| `scope` | `UserInfo.Scope` | `scope = 'api'` |
| `token_type` | `Snapshot.TokenType` | `token_type = 'BackendService'` |
| `name` | `UserInfo.Name` 或 `name` claim | `name like 'ben'` |
| `metadata.xxx` | `UserInfo.UserMetadatas` | `metadata.department = 'platform'` |
| `tenant.xxx` | `UserInfo.TenantMetadata` | `tenant.region = 'CN'` |

## 关于 `token_type`

这是本 skill 需要特别强调的新共识：

- JWT 标准链路里的 `token_type` claim，框架身份层会解析到 `AsgardIdentitySnapshot.TokenType`
- 现在 `AsgardAuth` 已经原生支持 `token_type` 字段，无需再手工把它复制到 `metadata.token_type`
- 推荐比较值直接使用枚举名：`UserLogin`、`BackendService`

示例：

```csharp
[AsgardAuthMatch("token_type = 'BackendService'")]
public async Task<ActionResult<Response<object>>> RunJobAsync()
{
    return Success("ok");
}
```

```csharp
[AsgardAuthMatch("token_type = 'UserLogin' and permission = 'orders.read'")]
public async Task<ActionResult<Response<OrderVo>>> GetAsync(string id)
{
    return Success(new OrderVo());
}
```

## 推荐写法

### 直接用内置特性

适合简单规则：

```csharp
[AsgardAuthAnyPermission("orders.read")]
[AsgardAuthAnyRole("admin", "ops")]
```

### 用 `AsgardAuthMatch(...)` 处理组合条件

适合需要同时判断多字段：

```csharp
[AsgardAuthMatch("token_type = 'BackendService' and scope = 'jobs.execute'")]
```

```csharp
[AsgardAuthMatch("(role = 'admin' or permission = 'tenant.audit.read') and tenant.region = 'CN'")]
```

### 仍然要做业务资源边界校验

就算 `AsgardAuth` 已通过，也不能省略这些业务判断：

- 路由里的 `tenantId` 是否等于当前身份租户
- 当前用户是否真的属于目标业务资源
- 平台管理员是否被允许跨租户访问

## 不要这样做

- ❌ 不要假设 JWT 任意 claim 都能直接写成 DSL 字段
- ❌ 不要把 `AsgardAuth` 当成资源归属校验器
- ❌ 不要为了判断 `token_type` 再额外复制一份 `metadata.token_type`，除非你有兼容旧版本的明确需求
- ❌ 不要扩展 DSL 字段却只改解析器，不补求值与测试
- ❌ 不要把 `type` 当成 `token_type` 的别名，当前原生字段名是 `token_type`

## 扩展实现检查清单

如果以后还要为 DSL 加新字段，至少要同步检查：

1. `AsgardAuthFieldKind` 是否新增枚举项
2. `AsgardAuthExpressionParser` 是否新增字段解析
3. `AsgardAuthEvaluator` 是否新增取值逻辑
4. 解析测试与求值测试是否同步补齐
5. skill / README 是否同步更新，避免知识只留在代码里

## 推荐协同 skill

- 需要设计 Heimdall BackendService 目录 API 的 Scope、Audience、租户边界和 Fail Closed：结合 `$heimdall-service-integration`
- 需要理解 claims 与身份快照来源时：结合 `$asgard-identity-userinfo`
- 需要写 Controller 与接口授权声明时：结合 `$asgard-api-development`
- 需要理解宿主认证、默认 JWT 与授权中间件接线时：结合 `$asgard-host-features`
- 需要全局路由判断时：结合 `$asgard-framework-overview`

## 源码锚点

- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - 全部 `AsgardAuth*` 特性
- `Common/Asgard.AspNetCore.Core/Authorization/AsgardAuthExpressionParser.Parser.cs` - DSL 字段解析
- `Common/Asgard.AspNetCore.Core/Authorization/AsgardAuthEvaluator.Resolution.cs` - 字段取值与比较
- `Common/Asgard.Abstractions/Identity/AsgardIdentitySnapshot.cs` - `TokenType` 所在身份快照
- `Common/Asgard.AspNetCore.Core/Identity/DefaultAsgardIdentityContextResolver.cs` - `token_type` claim 解析来源

