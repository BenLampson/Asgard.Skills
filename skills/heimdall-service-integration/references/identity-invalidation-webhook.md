# 身份失效 Webhook

## 目录

- [事件语义](#事件语义)
- [请求头](#请求头)
- [接收方处理顺序](#接收方处理顺序)
- [JWT 失效策略](#jwt-失效策略)
- [重试与幂等](#重试与幂等)
- [密钥生命周期](#密钥生命周期)
- [下游业务反应](#下游业务反应)
- [出站安全](#出站安全)

## 事件语义

事件类型：

```text
identity.subject.invalidated
version = 1
```

TenantUser 停用、软删除或管理员撤销时，Heimdall 在同一事务内推进主体撤销水位、撤销相关身份对象并写 Outbox。HTTP 请求由后台 Worker 投递，业务事务不直接依赖接收方在线。

规范主体 ID 为 `TenantUser.Id`，并与 JWT `sub`、事件 `subject_id`、用户资源 `id`、成员资源 `tenant_user_id` 一致。

载荷示例：

```json
{
  "event_id": "stable-event-id",
  "event_type": "identity.subject.invalidated",
  "version": 1,
  "occurred_at": "2026-07-18T10:00:00Z",
  "tenant_id": "tenant-id",
  "subject_id": "tenant-user-id",
  "revoked_at": "2026-07-18T10:00:00Z",
  "reason": "disabled|deleted|revoked",
  "operator_id": "operator-subject-id"
}
```

`reason` 只允许 `disabled`、`deleted`、`revoked`。消费者必须拒绝不支持的事件类型或版本；是否容忍新增字段应以当前 Heimdall JSON Schema 的 `additionalProperties` 约束为准，不得自行猜测兼容性。

Heimdall 仓库中的机器可读标准为 `docs/schemas/identity-subject-invalidated.schema.json`，采用 JSON Schema Draft 2020-12。数据库 Outbox 的 `schema_version` 仅用于内部持久化，不属于 HTTP JSON。

## 请求头

```http
X-Heimdall-Event-Id: <stable-event-id>
X-Heimdall-Timestamp: <unix-seconds>
X-Heimdall-Key-Id: <key-id>
X-Heimdall-Signature: sha256=<lowercase-hex>
```

签名输入是时间戳、点号和未经修改的原始请求体：

```text
payload   = timestamp + "." + rawRequestBody
signature = HMAC-SHA256(base64Decode(secret), UTF8(payload))
```

接收方必须对原始字节验签，不能先反序列化再重新序列化 JSON。

## 接收方处理顺序

1. 读取并保留原始请求体。
2. 校验必需请求头和支持的 Key ID。
3. 将 Timestamp 限制在允许偏差内，推荐正负 5 分钟。
4. 使用恒定时间比较验证 HMAC。
5. 以 Event ID 做幂等检查。
6. 在本地事务中推进该主体的 `revoked_at` 水位并执行必要业务处理。
7. 提交成功后返回 2xx；临时故障返回非 2xx 以触发重试。

水位只允许前进。接收乱序旧事件时可记录已消费 Event ID，但不得降低 `revoked_at`。

## JWT 失效策略

业务系统继续离线验证 JWT 签名、Issuer、Audience 和有效期，并额外查询本地主体撤销水位：

```text
reject when token.iat <= subject.revoked_at
```

Webhook 不替代正常 JWT 校验；它补足 JWT 尚未自然过期时的即时撤销。仍需短生命周期 Access Token 和定时目录对账作为纵深防御。

## 重试与幂等

- 网络错误、超时和非 2xx 使用持久化指数退避重试。
- 自动重试保持稳定 Event ID。
- 手动重投也保持原 Event ID，但使用新的 Timestamp 和签名。
- 接收方对重复 Event ID 返回成功，不重复执行不可逆业务操作。
- 监控最终失败、积压时长、连续签名失败和未知 Key ID。

## 密钥生命周期

- HMAC Key 在创建或轮换时只显示一次，存入 Secret Manager。
- 请求携带 Key ID，允许轮换期间短暂同时接受新旧 Key。
- 不把 Key 写入配置仓库、日志、工单或聊天。
- 泄露时立即轮换，缩短重叠窗口并审计失败验签来源。

## 下游业务反应

具体动作属于下游业务，但通常包括：

- 拒绝旧 JWT，停止新的授权或分配；
- 断开实时连接；
- 取消 Pending Offer；
- 回收进行中会话或转人工处理；
- 将业务 Profile 标记为 Suspended；
- 安排目录 API 对账确认最终状态。

这些动作不能反向改变 Heimdall 的 TenantUser 状态。

## 出站安全

- 生产订阅仅允许 HTTPS。
- 限制可访问的目标地址，防止 SSRF、私网探测和重定向绕过。
- 本机 HTTP 只允许明确的开发调试场景，生产必须关闭相关开关。
- 请求和响应日志应脱敏，不记录签名密钥、Authorization 或完整敏感正文。
