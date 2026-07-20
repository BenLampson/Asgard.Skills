# Heimdall 服务集成稳定不变量

本页用于设计和代码评审。面向接入方的完整操作说明见同目录其他 references。

## 身份与租户

- `TenantUser.Id = JWT sub = Webhook subject_id = 用户资源 id = 成员资源 tenant_user_id`。
- `TenantUid` 仅为历史内部关联键，不应出现在新外部契约中。
- 租户绑定资源只能依据已验证 Token 中的 `tenant_id` 查询。
- 跨租户资源按不存在处理，不回退全局查询。

## BackendService Token

目录读取固定使用：

| 项目 | 值 |
|---|---|
| Grant | `client_credentials` |
| Token type | `BackendService` |
| Scope | `heimdall.directory.read` |
| Audience | `heimdall-directory-api` |

必须同时验证签名、Issuer、Audience、Token Type、Scope 和 Tenant。BackendService 只读，不授予组织或目录写权限。

## 最终身份状态

查询、登录、Refresh Token、Introspection、目录 API 和对账必须调用同一最终状态计算。当前启用条件为：TenantUser 未删除，且至少存在一条未删除、启用的登录记录。

有效目录成员同时满足：成员关系未删除、组未删除且启用、用户未删除且最终状态启用。

## 用户权限快照

业务服务查询候选用户权限固定使用：

```http
GET /api/backend/directory/users/{tenantUserId}/permissions
```

- Tenant 只来自已验证 BackendService Token 的 `tenant_id`，路由不接受 Tenant 参数。
- 不存在、已删除或跨租户用户统一返回 404。
- 禁用用户返回 `status=Disabled` 和空权限集合。
- `permissions` 只包含未删除、已启用角色通过有效关系授予的未删除、已启用权限编码。
- `updatedAt` 必须覆盖用户状态、人工及外部受管角色授予、角色、角色权限关系和权限本身的变化。
- 权限解析异常返回 5xx；下游不得把错误、超时或陈旧正向缓存视为授权成功。

## 身份失效

停用、软删除和管理员撤销在同一数据库事务中：

1. 推进主体撤销水位；
2. 撤销 Token、Authorization、Code、Consent 和活动 Session；
3. 写持久化 Outbox；
4. 为启用订阅创建投递记录。

后台 Worker 投递稳定 `event_id` 的签名 Webhook。接收方验证签名与时间窗口、按 Event ID 幂等，并拒绝 `iat <= revoked_at` 的旧 JWT。

Webhook v1 外部载荷固定使用 `version=1`，失效原因只允许 `disabled`、`deleted`、`revoked`。Outbox 表中的 `schema_version` 是内部持久化字段，不属于 HTTP 契约。

## Client 失效

Client 状态不是管理端展示字段，而是 Token Endpoint 的强制运行态边界：

- 停用或删除 Client 后，协议运行态不得再解析出该 Client，新 Token 请求返回 OAuth `invalid_client`；
- 同一事务撤销该 Client 已签发的 Access/Refresh Token、Authorization、Authorization Code、Device Code 和活动 Session；
- 重新启用只恢复后续凭据申请，不恢复已经撤销的协议状态。

## 失败策略

- 自动授权、路由或对账无法确认身份状态时 Fail Closed。
- 目录缓存只允许短 TTL，并使用稳定 `updated_at` 判断变化。
- Webhook 网络错误、超时和非 2xx 必须持久化重试。
- 手动重投保持原 Event ID，但生成新的 Timestamp 和签名。
