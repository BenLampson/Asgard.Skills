# Heimdall 服务集成稳定契约

## 目录 API

推荐资源契约：

| 项目 | 稳定值 |
|---|---|
| Grant | `client_credentials` |
| Token type | `BackendService` |
| Scope | `heimdall.directory.read` |
| Audience | `heimdall-directory-api` |
| Tenant | 仅从已验证 Token 的 `tenant_id` 读取 |
| Page | `page >= 1` |
| Size | 默认 10，最大 500 |

推荐路由：

```text
GET /api/backend/directory/users
GET /api/backend/directory/groups/{groupId}
GET /api/backend/directory/groups/{groupId}/members
GET /api/backend/directory/groups/{groupId}/members/{tenantUserId}
```

只允许 GET。列表返回 `PageResponse<T>`，详情返回 `Response<T>`。提供稳定 `updated_at` 或完整 ETag。
调用方需要离线契约测试时，应交付聚焦这些路由的 OpenAPI 文件；仅添加生成标记不等于已经交付契约产物。

有效成员必须满足：关系未删除、组启用且未删除、用户未删除、用户最终身份状态启用。关系不存在时返回 `active=false`；用户或组不存在时返回 404。

## TenantUser 最终状态

最终状态必须由共享入口计算。当前规则为：至少存在一条未删除且启用的登录记录时才启用，否则停用。以下链路不得各自复制或推测状态：

- 单用户与分页查询
- 密码、Passkey、LDAP、OIDC 或 SAML 登录完成
- Refresh Token
- Introspection 和 Access Token 即时校验
- BackendService 目录 API
- 身份对账任务

必须有真实正向集成测试覆盖“启用用户 + 启用凭据 + 正确密码 => 登录成功”，同时覆盖停用、删除和孤儿登录记录失败关闭。

## Subject

新契约统一使用：

```text
TenantUser.Id
= JWT sub
= Webhook subject_id
= Token/Session subject_id
= Backend Directory API tenant_user_id
```

`TenantUid` 只作为历史内部关联键。兼容升级时，失效事务应同时撤销规范 `Id` 与历史 `TenantUid` 对应的旧凭据，但 Webhook 只输出规范 `Id`。

## 身份失效 Webhook

停用、软删除和管理员撤销必须在同一事务内：

1. 写入 Subject 撤销水位；
2. 撤销 Access Token、Refresh Token、Authorization、Code、Consent 和活动 Session；
3. 写入持久化 Outbox；
4. 为启用订阅创建投递记录。

HTTP 请求由后台 Worker 执行。签名输入使用：

```text
HMAC-SHA256(secret, timestamp + "." + raw_utf8_body)
```

请求至少携带稳定 Event ID、Unix Timestamp、Key ID 和 `sha256=` 签名。网络错误、超时和非 2xx 持久化指数退避重试；手动重投保持原 Event ID。

接收方必须恒定时间比较签名、限制 Timestamp 偏差、按 Event ID 幂等，并维护每个主体的 `revoked_at` 水位，拒绝 `iat <= revoked_at` 的旧 JWT。

## 验收

- 验证没有接受任意 `tenantId` 的 BackendService 路由。
- 验证 UserLogin Token、错误 Scope、错误 Audience 和无租户 Token 均返回 403。
- 验证跨租户 ID 返回 404，不回退全局查询。
- 验证成员停用或组停用后立即返回 `active=false`。
- 验证停用与 Outbox 在同一事务提交或回滚。
- 验证 Webhook 稳定 Event ID、签名、防重放和非 2xx 重试。
- 验证短 TTL 缓存过期且 Heimdall 不可用时下游 Fail Closed。
