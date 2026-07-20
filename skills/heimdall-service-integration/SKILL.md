---
name: heimdall-service-integration
description: Heimdall 微服务身份集成与交付 skill。Use when designing, implementing, documenting, reviewing, or accepting tenant-bound BackendService client_credentials, read-only directory and user-permission APIs, permission-gated ticket assignment, TenantUser effective status, subject invalidation Webhooks, JWT revocation propagation, client secret rotation, or service-side identity reconciliation against Heimdall.
---

# Heimdall Service Integration

## 作用

把 Heimdall 作为身份系统可靠地接入后端微服务。覆盖服务 Client、Token、只读目录、主体失效、身份对账和正式交付，不承接下游业务域模型。

## 先读什么

- 所有任务先读 `references/integration-guide.md`，确认当前已交付能力、待补能力和职责边界。
- 涉及 Client 创建、`client_credentials`、Secret 保存或轮换时，读 `references/client-credentials-lifecycle.md`。
- 涉及自动路由、候选客服权限、目录组、成员校验或定时对账时，读 `references/backend-directory-api.md`。
- 涉及停用、删除、撤销、JWT 失效或 Webhook 消费时，读 `references/identity-invalidation-webhook.md`。
- 涉及上线、联调、版本声明或验收时，读 `references/end-to-end-acceptance.md`。
- 评审底层设计不变量时，读 `references/service-contract.md`。

相关实现 skill：

- Controller 和响应模型：`$asgard-api-development`
- 授权表达式：`$asgard-auth-authorization`
- Claim、`sub`、BackendService Token：`$asgard-identity-userinfo` 与 `$identity-integration`
- Heimdall C#：`$asgard-dotnet-10-csharp-14`

## 实施规则

1. 固定主体标识：`TenantUser.Id = JWT sub = Webhook subject_id = 用户资源 id = 成员资源 tenant_user_id`。
2. TenantUser 最终状态使用同一个共享计算入口，供查询、登录、Refresh Token、Introspection 和目录 API 调用。
3. 微服务只使用独立只读目录 Controller；不要复用管理员读写接口。
4. 同时校验签名、Issuer、Audience、`token_type=BackendService`、Scope 和 Token 中可信 `tenant_id`。
5. 租户绑定接口不接受 Route、Query 或 Body 中的 `tenantId`；默认禁止平台服务跨租户。
6. 停用、删除和管理员撤销必须在同一事务中撤销身份并写 Outbox，再异步投递 Webhook。
7. 下游结合短生命周期 JWT、Webhook 撤销水位、短 TTL 目录缓存和定时对账；授权依据不可确认时 Fail Closed。
8. Client 停用或删除必须立即撤销其 Token、Authorization、Code 和活动 Session，并让 Token Endpoint 按 OAuth 标准返回 `invalid_client`。
9. 身份失效 Webhook v1 对外固定使用 `version=1` 和 `reason=disabled|deleted|revoked`；数据库内部 `schema_version` 不得泄漏为 HTTP 字段。
10. `sys_users.display_name` 与 `sys_users.is_built_in` 是正式业务字段：前者分离登录名与展示名，后者保护内置管理员；数据库清理或回滚时不得把它们当作遗留列删除。
11. 自动分派候选人必须通过 `GET /api/backend/directory/users/{tenantUserId}/permissions` 获取最终状态和有效权限；只有 `status=Active` 且全部业务必需权限同时存在时才允许继续。
12. 权限接口超时、网络错误、401/403/404/429、5xx、响应无法解析或权限状态不确定时必须 Fail Closed；不得把异常降级为空权限成功，也不得用过期的正向缓存继续自动分派。

## 文档与交付规则

外部契约发生变化时，在同一批变更中完成：

1. 更新实现和自动化测试。
2. 更新本 skill 对应 reference，明确“已交付”或“待补”，不得把设计建议写成现有能力。
3. 更新 Heimdall 仓库中的 OpenAPI、示例请求和必要配置说明。
4. 运行真实的 `Client -> Token -> API/Webhook` 端到端验收。
5. 给出正式 commit/tag、不可变镜像版本与 digest、部署环境和租户 Client 交付信息。

仅存在本地代码、单元测试或 `latest` 镜像时，不得宣称已经正式交付。

## 边界

- Heimdall 负责租户、用户、Client、Scope、RBAC、目录和身份生命周期。
- 下游负责业务 Profile、能力、路由、队列、会话、工单和订阅状态。
- BackendService 不得获得目录写权限。
- 下游不得直查 Heimdall 数据库，不得信任浏览器提交的成员关系。
- OIDC `/userinfo` 不是完整目录或最终身份状态接口。
