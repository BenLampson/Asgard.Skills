---
name: heimdall-application-rbac
description: Heimdall 应用域 RBAC skill。Use when designing, implementing, migrating, reviewing, or debugging Application Manifest permissions and roles, TenantApplication bindings, SystemUser application grants, application-scoped Tenant RBAC or OIDC Clients, application/version JWT claims, or multi-application tenant visibility and authorization boundaries in Heimdall.
---

# Heimdall Application RBAC

## 作用

本 skill 固化 Heimdall 的应用目录、应用权限模板、Tenant 应用绑定和应用管理员授权边界。它负责回答“谁能看到哪个应用和 Tenant、谁能开通或停用、权限如何同步、Token 属于哪个应用”，不侵入业务应用自己的管理层。

## 使用方式

1. 设计、实现或迁移前，完整读取 `references/domain-contract.md`。
2. 代码审查或验收前，完整读取 `references/review-checklist.md`。
3. 涉及 Claim 时结合 `$asgard-identity-userinfo`；涉及 mini issuer 时结合 `$asgard-mini-jwt-issuer`；涉及 Controller 与授权特性时结合 `$asgard-api-development` 和 `$asgard-auth-authorization`。

## 不可破坏的约束

- Heimdall 自身也是应用，但属于不可删除的系统内置应用。
- 应用权限只定义一次；Manifest 角色模板引用权限，Tenant 内置角色实例再引用这些权威权限记录。
- Tenant 与应用只有未开通、启用、停用等状态，不提供解除关系或物理删除；二次启用恢复原关系和数据。
- 应用管理员默认不得枚举未绑定或未授权 Tenant；开通已有 Tenant 必须基于准确 Tenant Code、邀请或有权主体确认。
- 授权必须同时满足：原生能力权限、SystemUser Application Grant、Tenant scope。平台超管单独明确旁路。
- `platform.application.tenant.manage`、`platform.application.tenant_rbac.manage`、`platform.application.oidc_client.manage` 是应用范围权限，不等于对应全局权限。
- 创建新 Tenant 仍要求 Heimdall 原生 `platform.tenant.manage`，应用角色不能替代它。
- Manifest 版本单调递增；Tenant 记录实际成功应用的版本，不能把应用当前版本冒充为 Tenant 已同步版本。
- 一枚业务 Access Token 只属于一个 `application_id`；业务应用 Token 不得包含 `platform.*`。
- 唯一索引不得把 `Deleted`、状态或其他生命周期字段拼入业务唯一键。删除后重建同 code 必须恢复原记录或被稳定业务键拒绝，不能制造重复脏数据。
- 批量更新、关系变更和同步必须维护乐观锁 `Version` 与授权版本。
- 无法确认应用、Client、Tenant 绑定、同步版本或授权范围时必须 Fail Closed。

## 工作流

### 1. 先写授权公式

对每个入口明确 Actor、Action、Application、Tenant 和数据状态，再写出服务端判定公式。不要只用前端菜单可见性或一个宽泛角色代替资源级守卫。

### 2. 区分三层数据

- 应用定义层：Application、Manifest、Permission、Role Template。
- Tenant 实例层：TenantApplication、内置角色实例、自定义角色与用户授权。
- 管理授权层：SysUserApplicationGrant 与精确 Tenant scope。

三层之间使用稳定 ID 关联；不要通过复制 permission code 推断身份，也不要让 Tenant 自定义角色反向修改 Manifest。

### 3. 审查生命周期

逐项验证发布 Manifest、开通已有 Tenant、创建新 Tenant、同步、停用、重新启用、应用授权撤销和历史数据迁移。停用应阻断签发与业务访问，但保留可审计、可恢复的数据。

### 4. 审查 Token

应用切换通过目标应用的授权码/Token 流程重新签发；SSO 会话有效时无需重新登录。版本 Claim 是授权快照的失效依据，只做精确比较。

### 5. 用测试矩阵验收

至少覆盖平台超管、全局管理员、应用管理员、Tenant 管理员、普通用户，以及未绑定、启用、停用、越权应用、越权 Tenant 和旧版本 Token。

## 与其他 skill 的边界

| 问题 | 使用 |
|------|------|
| 应用 Manifest、Tenant 绑定、管理员 Grant 和授权版本 | `$heimdall-application-rbac` |
| Claim 字段和运行时 UserInfo | `$asgard-identity-userinfo` |
| mini JWT 签发、discovery、JWKS | `$asgard-mini-jwt-issuer` |
| OIDC、PKCE、SSO 与应用切换 | `$identity-integration` |
| BackendService 目录和身份失效集成 | `$heimdall-service-integration` |

## 不要这样做

- 不要给应用管理员发全公司级 Tenant、RBAC 或 OIDC Client 管理权限。
- 不要允许应用管理员通过普通列表枚举未绑定 Tenant。
- 不要把 `AllApplicationTenants` 写进 Token；它是服务端 Grant 语义并自动覆盖未来绑定 Tenant。
- 不要删除 TenantApplication 后重新部署一套数据。
- 不要在业务应用 Token 中聚合多个应用的角色和权限。
- 不要用 `Deleted` 参与唯一索引以“允许重复 code”。
- 不要只迁移活跃行；软删除 OIDC Client 和历史关系也可能受新非空外键约束影响。
- 不要让 mini issuer 自己查询或推导 Heimdall 授权；调用方必须传入已裁剪且有权威版本的快照。
