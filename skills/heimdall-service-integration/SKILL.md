---
name: heimdall-service-integration
description: Heimdall 微服务身份集成 skill。Use when designing or implementing BackendService directory APIs, tenant-bound client_credentials access, TenantUser effective status, subject invalidation Webhooks, JWT revocation propagation, or service-side identity reconciliation against Heimdall.
---

# Heimdall Service Integration

## 作用

用于把 Heimdall 作为身份系统接入后端微服务，覆盖服务令牌、只读目录、主体失效和身份对账，不承接下游业务域模型。

## 开始前

- 读取 `references/service-contract.md`，以其中的稳定契约作为实现和评审基线。
- 编写 Controller 时同时使用 `$asgard-api-development`。
- 编写授权表达式时同时使用 `$asgard-auth-authorization`。
- 调整 claim、`sub` 或 BackendService Token 时同时使用 `$asgard-identity-userinfo` 和 `$identity-integration`。
- 编写 Heimdall C# 代码时必须使用 `$asgard-dotnet-10-csharp-14`。

## 实施流程

1. 先固定外部主体标识：`TenantUser.Id = JWT sub = Webhook subject_id = API tenant_user_id`。
2. 将 TenantUser 最终状态收敛为一个共享计算入口，供查询、登录、Refresh Token、Introspection 和目录 API 使用。
3. 为微服务提供独立只读 Controller，不复用管理员读写 Controller。
4. 同时校验签名、Issuer、Audience、`token_type=BackendService`、Scope 和 Token 中可信 `tenant_id`。
5. 租户绑定接口不要接受 Route、Query 或 Body 中的 `tenantId`；一期默认禁止平台服务跨租户。
6. 对停用、删除和管理员撤销，在同一数据库事务中撤销身份并写 Outbox，再异步投递 Webhook。
7. 使用稳定 `event_id`、HMAC-SHA256、Timestamp、重放窗口、幂等消费和非 2xx 持久化重试。
8. 下游使用短生命周期 JWT、Webhook 撤销水位、短 TTL 目录缓存和定时对账；上游不可用时自动授权与路由必须 Fail Closed。

## 边界

- Heimdall 负责租户、用户、Client、Scope、RBAC、目录和身份生命周期。
- 下游系统负责自己的业务 Profile、能力、路由、队列、会话、工单和订阅状态。
- 不要让 BackendService 获得目录写权限。
- 不要直接查询 Heimdall 数据库，也不要信任浏览器提交的成员关系。
- 不要把 OIDC `/userinfo` 当作完整目录或最终身份状态接口。
