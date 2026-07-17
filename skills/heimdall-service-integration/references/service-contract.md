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

## 身份失效

停用、软删除和管理员撤销在同一数据库事务中：

1. 推进主体撤销水位；
2. 撤销 Token、Authorization、Code、Consent 和活动 Session；
3. 写持久化 Outbox；
4. 为启用订阅创建投递记录。

后台 Worker 投递稳定 `event_id` 的签名 Webhook。接收方验证签名与时间窗口、按 Event ID 幂等，并拒绝 `iat <= revoked_at` 的旧 JWT。

## 失败策略

- 自动授权、路由或对账无法确认身份状态时 Fail Closed。
- 目录缓存只允许短 TTL，并使用稳定 `updated_at` 判断变化。
- Webhook 网络错误、超时和非 2xx 必须持久化重试。
- 手动重投保持原 Event ID，但生成新的 Timestamp 和签名。
