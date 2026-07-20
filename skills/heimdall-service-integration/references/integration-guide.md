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
| Heimdall 版本 | `5.2.0` |
| Git commit | `fdd1158e11a346cca3940e18e416acf1cc07b834` |
| Git tag | `v5.2.0` |
| Docker | `registry.cn-hangzhou.aliyuncs.com/benlampson/asgard.heimdall:5.2.0` |
| Image digest | `sha256:ae1fc20b3ca4e1832f3b207a5a9e64ee5f00c6a4f0d3ebd435e8c194e85c1685` |
| 正式站部署 | 2026-07-20 12:04（Asia/Shanghai），后端固定上述 digest，前端来自同一 commit |

部署时优先固定 `registry.cn-hangzhou.aliyuncs.com/benlampson/asgard.heimdall:5.2.0@sha256:ae1fc20b3ca4e1832f3b207a5a9e64ee5f00c6a4f0d3ebd435e8c194e85c1685`，不要把可变的 `latest` 当作验收版本。镜像已发布不等于某个开发或生产环境已完成部署，环境状态需要单独记录。

`5.2.0` 正式镜像已包含以下能力：

- Client 停用或删除后阻止新 Token，并原子撤销 Access/Refresh Token、Authorization、Authorization Code、Device Code 和活动 Session；
- Webhook HTTP JSON 固定使用 `version=1` 和 `reason=disabled|deleted|revoked`，并提供 JSON Schema Draft 2020-12；
- 提供创建测试 Tenant、用户、目录组和 BackendService Client 的联调 Fixture 脚本。
- 提供 `GET /api/backend/directory/users/{tenantUserId}`，用于 Tenant 边界内按 ID 查询用户最终身份状态。
- 提供 `GET /api/backend/directory/users/{tenantUserId}/permissions`，用于 Tenant 边界内读取候选用户最终状态和有效权限；禁用用户返回空权限，解析异常 Fail Closed。
- 合并平台用户显示名与登录名分离、内置管理员保护；`sys_users.display_name` 和 `sys_users.is_built_in` 均为正式字段，不得作为遗留数据删除。

发布方验证记录：后端 Release 测试 `504/504`、前端 TypeScript 检查、ESLint 与生产构建通过，并已从 Registry 核对 `5.2.0` 与 `latest` 指向上述相同 digest。这些自动化验证不替代接入方真实端到端验收。

正式站部署后已确认：Heimdall 容器运行上述不可变 digest 且重启次数为 0；前端 `index.html` 的构建、服务器文件和公网响应 SHA-256 一致；Issuer Discovery 返回 HTTP 200；Backend Directory 用户权限路由在无 Token 时返回 HTTP 401，证明路由已加载且认证边界生效。该冒烟验证仍不替代带真实 Tenant Client 的端到端验收。

当前发布已交付 OpenAPI `1.2.0` 静态契约 `docs/openapi/backend-directory-api.openapi.yaml`，但尚未完成目标接入环境的真实 `Client -> Token -> Directory API -> identity.subject.invalidated Webhook` 联调。接入方应固定上述 digest，并按 `end-to-end-acceptance.md` 执行联调；记录完成前只能声明“发布制品已交付”，不能声明“目标环境验收完成”。

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
7. 对需要权限门禁的候选用户调用 `/users/{tenantUserId}/permissions`，仅在用户 Active 且全部必需权限存在时继续业务动作。
8. 配置身份失效 Webhook 订阅，保存一次性显示的签名密钥。
9. 消费方实现签名、防重放、幂等和主体 `revoked_at` 水位。
10. 增加短 TTL 缓存、定时全量对账和 Heimdall 不可用时的 Fail Closed。
11. 按端到端验收清单完成真实环境联调。

## 当前能力矩阵

| 能力 | 状态 | 说明 |
|---|---|---|
| BackendService Token | 已交付 | Tenant-bound `client_credentials` |
| 用户分页 | 已交付 | 可用于全量身份对账 |
| 目录组详情 | 已交付 | 返回组状态和更新时间 |
| 目录组成员分页 | 已交付 | 可拉取组内候选人 |
| 单成员有效性校验 | 已交付 | 适合自动路由逐候选人 Fail Closed |
| 单 TenantUser 查询 | 已发布（5.1.1，5.2.0 保留） | `GET /api/backend/directory/users/{tenantUserId}`，跨租户按不存在处理 |
| TenantUser 有效权限查询 | 已发布（5.2.0） | `GET /api/backend/directory/users/{tenantUserId}/permissions`，用于候选权限门禁 |
| 身份失效 Webhook | 已交付 | Outbox、签名、防重放、重试 |
| Client Secret 轮换 | 已交付 | 支持 0–1440 分钟旧 Secret 重叠窗口 |
| Client 停用/删除即时撤销 | 已发布（5.1.0） | 阻止新 Token，并原子撤销已签发协议状态 |
| Webhook `version/revoked` 契约 | 已发布（5.1.0） | 以 JSON Schema Draft 2020-12 固化 |
| 联调 Fixture 脚本 | 已发布（5.1.0） | 创建 Tenant、用户、组和 BackendService Client |
| 5.2.0 OpenAPI 静态快照 | 已交付 | OpenAPI `1.2.0`，随源码和固定镜像版本交付 |
| 目标环境真实端到端记录 | 待接入验收 | 覆盖 M2M、目录、用户权限、跨租户拒绝和身份失效回调 |

调用方必须使用单用户接口校验指定 TenantUser；需要权限门禁时必须使用用户权限接口。不能为了校验一个用户遍历整个租户分页，也不能跳过身份或权限校验。

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
