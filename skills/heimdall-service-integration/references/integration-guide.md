# Heimdall 微服务接入指南

## 目录

- [定位与职责](#定位与职责)
- [当前交付基线](#当前交付基线)
- [推荐接入架构](#推荐接入架构)
- [接入步骤](#接入步骤)
- [当前能力矩阵](#当前能力矩阵)
- [配置与环境交付](#配置与环境交付)
- [文档导航](#文档导航)

## 定位与职责

Heimdall 是身份与访问管理系统，向业务微服务提供：

- 租户、TenantUser、登录凭据和最终身份状态；
- Confidential Client、Scope、Audience 和 BackendService Token；
- 只读组织目录和成员关系；
- 停用、删除、撤销后的可靠失效事件；
- 管理端 RBAC 和组织管理。

业务系统继续拥有自己的 AgentProfile、技能、接待能力、渠道、访客、会话、消息、路由队列、Offer、工单、SLA、套餐、额度、订阅、AI 配置、知识库、通知、分析和审计。

业务状态不能反向替代身份状态。例如客服系统可以暂停 AgentProfile，但不能因此修改 Heimdall TenantUser；Heimdall 停用 TenantUser 时，客服系统必须停止其新业务活动。

## 当前交付基线

| 项目 | 正式值 |
|---|---|
| Heimdall 版本 | `5.0.0` |
| Git commit | `48758fd` |
| Git tag | `v5.0.0` |
| Docker | `registry.cn-hangzhou.aliyuncs.com/benlampson/asgard.heimdall:5.0.0` |
| Image digest | `sha256:392e1764591a1d05b7f3f635d308042877deb6cdfa3849713c649323b890bb65` |

部署时优先固定 `registry.cn-hangzhou.aliyuncs.com/benlampson/asgard.heimdall:5.0.0@sha256:392e1764591a1d05b7f3f635d308042877deb6cdfa3849713c649323b890bb65`，不要把可变的 `latest` 当作验收版本。镜像已发布不等于某个开发或生产环境已完成部署，环境状态需要单独记录。

当前 Heimdall 工作树已实现但尚未形成新 commit/tag/镜像的契约修订：Client 停用/删除即时撤销全部协议状态；Webhook HTTP JSON 使用 `version=1` 和 `reason=disabled|deleted|revoked`；增加 JSON Schema 与联调数据准备脚本。这些内容在新版本发布前属于“源码已实现、正式交付待发布”，不得归入上表的 5.0.0 镜像能力。

更新 Heimdall 外部契约或发布版本时，必须同步更新此表。

## 推荐接入架构

```text
Tenant-bound Confidential Client
        |
        | client_credentials
        v
Heimdall /connect/token
        |
        | BackendService JWT
        v
Backend Directory API --------> 短 TTL 缓存 / 定时对账

Heimdall 身份事务
        |
        | Outbox + HMAC Webhook
        v
业务系统撤销水位 --------> 拒绝旧 JWT / 断开连接 / 回收业务状态
```

目录 API 解决“现在是否有效”，Webhook 解决“状态变更后立即通知”，定时对账解决投递、消费或短暂故障后的最终一致性。三者共同构成身份闭环。

## 接入步骤

1. 为每个 Tenant 创建独立 Confidential Client。
2. 允许 `client_credentials`，授予最小 Scope `heimdall.directory.read`。
3. 将 Client 绑定单一 Tenant；业务系统按 Tenant 安全保存 Client ID 和 Secret。
4. 从 `/connect/token` 获取 BackendService Token。
5. 验证 Token 包含正确的 `tenant_id`、`token_type`、Scope 和 Audience。
6. 使用 Token 调用只读目录 API，不接受浏览器传入的成员关系作为授权事实。
7. 配置身份失效 Webhook 订阅，保存一次性显示的签名密钥。
8. 消费方实现签名、防重放、幂等和主体 `revoked_at` 水位。
9. 增加短 TTL 缓存、定时全量对账和 Heimdall 不可用时的 Fail Closed。
10. 按端到端验收清单完成真实环境联调。

## 当前能力矩阵

| 能力 | 状态 | 说明 |
|---|---|---|
| BackendService Token | 已交付 | Tenant-bound `client_credentials` |
| 用户分页 | 已交付 | 可用于全量身份对账 |
| 目录组详情 | 已交付 | 返回组状态和更新时间 |
| 目录组成员分页 | 已交付 | 可拉取组内候选人 |
| 单成员有效性校验 | 已交付 | 适合自动路由逐候选人 Fail Closed |
| 单 TenantUser 查询 | 待补 | 目标路由 `GET /api/backend/directory/users/{tenantUserId}` |
| 身份失效 Webhook | 已交付 | Outbox、签名、防重放、重试 |
| Client Secret 轮换 | 已交付 | 支持 0–1440 分钟旧 Secret 重叠窗口 |
| Client 停用/删除即时撤销 | 已实现待发布 | 阻止新 Token，并原子撤销已签发协议状态 |
| Webhook `version/revoked` 契约 | 已实现待发布 | 以 JSON Schema Draft 2020-12 固化 |
| 联调 Fixture 脚本 | 已实现待发布 | 创建 Tenant、用户、组和 BackendService Client |

单用户接口补齐前，不能为了校验一个用户遍历整个租户分页。若业务操作必须确认 TenantUser，调用方应暂时拒绝该操作或使用经过明确评审的受限替代流程，不能跳过身份校验。

## 配置与环境交付

每个接入环境至少交付以下非敏感信息：

- Heimdall Issuer 和 Token Endpoint；
- Directory API Base URL；
- Webhook 出站来源与网络要求；
- Tenant ID、Client ID、Scope 和 Audience；
- 部署 commit、tag、镜像版本和 digest；
- OpenAPI 文件位置和契约版本；
- 联调时间、测试 Tenant 和负责人。

Secret 与 Webhook HMAC Key 只能通过安全渠道或 Secret Manager 交付，不写入文档、工单、聊天、日志或普通业务数据库。

## 文档导航

- Client 创建、取 Token、Secret 轮换：`client-credentials-lifecycle.md`
- 目录路由、字段、缓存、错误策略：`backend-directory-api.md`
- Webhook 事件、签名、消费策略：`identity-invalidation-webhook.md`
- 真实端到端和发布验收：`end-to-end-acceptance.md`
- 稳定设计不变量：`service-contract.md`
