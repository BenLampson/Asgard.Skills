# Heimdall 微服务集成验收

## 目录

- [真实 Token 与目录链路](#真实-token-与目录链路)
- [身份失效链路](#身份失效链路)
- [Client 轮换链路](#client-轮换链路)
- [交付清单](#交付清单)
- [不算正式交付的情况](#不算正式交付的情况)

## 真实 Token 与目录链路

验收必须连接实际运行的 Heimdall，不以 Controller 单元测试代替：

```text
创建 Tenant
-> 创建绑定该 Tenant 的 Confidential Client
-> 允许 client_credentials
-> 授予 heimdall.directory.read
-> POST /connect/token
-> 验证 tenant_id/token_type/scope/aud
-> 调用户分页、组详情、成员分页和单成员校验
-> 使用其他 Tenant 的资源 ID，确认不可读取
```

至少覆盖：

- 正确 Token 成功；
- UserLogin Token、错误 Scope、错误 Audience、无 Tenant Token 返回 403；
- Route/Query/Body 无法覆盖 Token Tenant；
- 跨租户 ID 返回 404，不泄露存在性；
- 组停用、用户停用、关系删除后 `active=false`；
- 分页边界、最大 size 和稳定 `updated_at`；
- Heimdall 不可用或响应不确定时调用方 Fail Closed。

单用户查询交付后，增加存在、停用、删除、跨租户和组为空时 Profile 创建/启用的验收。

## 身份失效链路

```text
启用用户并获得 JWT
-> 停用或撤销 TenantUser
-> 确认 Session/Refresh Token 等被撤销
-> 确认 Outbox 与身份事务原子提交
-> 接收签名 Webhook
-> 推进下游 revoked_at
-> 使用旧 JWT，确认被拒绝
```

至少覆盖：

- Event ID 稳定且重复消费幂等；
- 正确签名成功，正文篡改、错误 Key、过期 Timestamp 失败；
- 网络错误和非 2xx 自动重试；
- 手动重投保持 Event ID，更新时间戳和签名；
- 乱序事件不会降低撤销水位；
- 下游断开连接、停止新分配并回收业务状态；
- Webhook 丢失模拟后，定时目录对账能够纠正状态。

## Client 轮换链路

1. 使用旧 Secret 成功取 Token。
2. 以非零宽限期轮换并安全保存新 Secret。
3. 宽限期内验证新、旧 Secret 均符合预期。
4. 更新所有服务实例，使用新 Secret 调用目录 API。
5. 宽限期后验证旧 Secret 失败、新 Secret 成功。
6. 再以 0 分钟轮换，验证旧 Secret 立即失效。

不要只检查管理接口返回 200；必须实际调用 Token Endpoint。

## 交付清单

正式交付应同时提供：

- 已推送的 commit 和正式 tag；
- 不可变 Docker 版本与 digest；
- 目标环境的实际部署版本；
- 与实现一致的 OpenAPI、请求示例和本 skill references；
- Tenant ID、Client ID、Scope、Audience 与安全的 Secret 交付方式；
- Webhook URL、Key ID、一次性密钥交付和出站网络要求；
- 自动化测试结果和真实端到端验收记录；
- 回滚方式、轮换方式和故障联系人。

部署验收建议记录：

```text
environment:
heimdall_commit:
heimdall_tag:
image:
image_digest:
issuer:
directory_base_url:
tenant_id:
client_id:
scope:
audience:
webhook_key_id:
openapi_version:
tested_at:
tested_by:
```

不要把 Client Secret、Webhook Secret 或完整 Access Token 写入验收记录。

## 不算正式交付的情况

- 代码只在本地工作树，没有 commit/tag。
- 只有单元测试，没有真实 Token Endpoint 和跨租户测试。
- 只推送了 `latest`，没有版本 tag 或 digest。
- 镜像已推送，但目标环境仍运行旧版本。
- 接口存在，但没有 Tenant Client 的创建、保存和轮换方案。
- 文档把计划中的单用户接口写成已经可用。
- Webhook 有 Controller 或 DTO，但没有 Outbox、重试、签名和接收方验收。
