# Heimdall MCP Contract

## 目录

1. [传输与认证](#传输与认证)
2. [凭据策略](#凭据策略)
3. [服务复用与实现地图](#服务复用与实现地图)
4. [工具契约](#工具契约)
   - [新增租户用户](#新增租户用户)
5. [安全边界](#安全边界)
6. [协议原语](#协议原语)
7. [验证清单](#验证清单)

## 传输与认证

- 端点：`/mcp`
- 传输：MCP Streamable HTTP，使用有状态会话以支持 Tasks 和协议交互。
- OAuth：`Authorization: Bearer <access_token>`；受保护资源元数据位于 `/.well-known/oauth-protected-resource/mcp`。
- AK/SK：HTTP Basic，用户名为 AK、密码为 SK；也支持 `X-Heimdall-Access-Key` 与 `X-Heimdall-Secret-Key`。
- 平台 AK 前缀：`hmcp_p_`；租户 AK 前缀：`hmcp_t_`。
- Bearer token 必须由 Heimdall 内置管理应用签发，并继续受 Asgard `roles`、`permissions`、`scope`、`token_type` 和 Tenant 声明约束。该应用边界必须放在 `/mcp` 端点级授权策略，覆盖 Tools、Resources、Prompts 与 Tasks。

客户端优先通过 MCP `tools/list`、`resources/list`、`prompts/list` 动态发现能力，不依赖静态工具清单。`tools/list` 反映实际连接可发现的工具；`heimdall_get_mcp_credential_catalog` 返回该身份作用域可配置的目录，不能用于证明某把凭据可以调用某个工具。

## 凭据策略

`McpCredentialPolicyDto` 包含：

- `allowedTools`：工具白名单。
- `allowedPermissions`：用户实时业务权限的上限；认证时取交集。
- `allowedCidrs`：IPv4、IPv6、精确地址或 CIDR 来源白名单。
- `expiresAt`：UTC 失效时间。
- `environment`：`production`、`staging` 或 `development` 等审计标签。
- `rateLimitPerMinute`：凭据认证固定窗口额度。
- `maxConcurrentCalls`：工具并发上限。

空工具或权限列表表示“不额外收窄”，用于兼容已有凭据。非空 `allowedTools` 只允许其中明确列出的工具，升级部署时不会自动加入新工具。停用、过期、来源不匹配或超限时认证失败。

用凭据所有者的 Heimdall 管理应用 `UserLogin` 身份从账号中心（`/account?section=mcp`）编辑策略，或在可用时调用 `heimdall_update_mcp_credential_policy`。保留原有白名单并按需加入单项工具，保存后重新连接 MCP、调用 `tools/list` 核实。AK/SK 不能修改自身或其他凭据，也不能拿去调用普通 REST API。委派仍不得超过调用身份的委派边界。

## 服务复用与实现地图

业务能力必须调用已有 `Services/IServices`：

- 平台：Tenant、SystemUser、现有租户内创建用户、Platform RBAC、Application、Manifest、Grant、Runtime/Security/Object Storage Settings、TOTP、Security Event、SIEM Export/Lifecycle、System Log、Trace。
- 租户：User、Profile、Metadata、Directory、Application RBAC、OIDC Client/Scope/Key/Key Log、OIDC/LDAP/SAML Provider、External Group Mapping、Authorization、Session、Token Blacklist、SCIM、Webhook、TOTP。
- MCP 治理：Credential Catalog/Lifecycle、Policy、Audit、Confirmation、Rate/Concurrency、Resources、Prompts、Tasks。

MCP 专属代码位于 Heimdall：

- `Mcp/Infrastructure/`：身份边界、网络策略、限流、并发、确认、工具过滤与审计。
- `Mcp/Platform/`：平台读写工具。
- `Mcp/Tenant/`：租户读写工具。
- `Mcp/Resources/`、`Mcp/Prompts/`：协议资源和可复用工作流提示。
- `Services/Services/Identity/McpCredentialService.cs`：AK/SK 生命周期与策略认证。
- `Services/Services/Identity/McpToolCatalogService.cs`：管理 UI 动态工具/权限目录。

## 工具契约

- 工具名使用小写 snake_case 和稳定前缀。
- 只读工具：`ReadOnly=true`、`Idempotent=true`。
- 写工具：参数必须包含可空 `confirmationToken`。
- 高影响写操作：`Destructive=true`。
- 创建、轮换、重投等操作按实际语义设置 `Idempotent=false`。
- 结构化返回：`UseStructuredContent=true`。
- 分页返回：`McpPageResult<T>`。
- 写返回：`McpMutationResult<T>`；第一次返回预览和令牌，第二次才执行。

### 新增租户用户

| 操作 | 工具 | 身份与范围 |
|---|---|---|
| 为指定的已有租户建号 | `platform_create_tenant_user`（Heimdall 5.12.1 起） | 平台身份，需 `platform.admin` 或 `platform.tenant_user.manage`；`dto.tenantId` 明确指定已有租户 |
| 为本租户建号 | `tenant_create_user` | 租户身份，需 `tenant.user.manage` 或 `tenant.workspace.admin`；`dto.tenantId` 必须匹配认证租户 |
| 开通新租户和首任管理员 | `platform_onboard_tenant` | 平台工具，单独的开通操作 |
| 创建平台系统用户 | `platform_create_system_user` | 平台工具，建立系统账号 |

只管理自身租户的接入方使用租户凭据。业务服务以 `client_credentials` 为本应用供给普通用户时，使用 [Heimdall 服务接入](../../heimdall-service-integration/SKILL.md) 的供给 API。

平台工具未出现在客户端时，按顺序核对已部署版本、`heimdall_whoami` 返回的身份范围及实际权限、凭据 `allowedTools`，再用同一凭据重新执行 `tools/list`。即使已有 `platform.tenant_user.manage`，非空白名单仍需加入 `platform_create_tenant_user`。平台授权不会让 `tenant_` 工具在平台连接出现。

首次 `tools/call` 的 `params` 示例：

```json
{
  "name": "platform_create_tenant_user",
  "arguments": {
    "dto": {
      "tenantId": "<现有租户ID>",
      "name": "sample.member",
      "email": "sample.member@example.test",
      "status": 1
    }
  }
}
```

从 `structuredContent` 读取 `requiresConfirmation: true`、预览、`confirmationToken` 和 `confirmationExpiresAt`。核对目标租户和用户名并获得操作者确认后，用同一凭据重发原样 `dto`，把令牌放在 `arguments.confirmationToken`。成功响应为 `requiresConfirmation: false`，`data` 包含已创建用户的 `id` 和 `tenantId`。令牌五分钟内单次有效；更换租户、用户资料或凭据后须重新预览。若需设置初始密码，`dto.password` 是请求中的可选字段，也绑定到确认令牌；预览与结果不回显密码，集成方不要记录请求中的密码。

### 用户与 TOTP 删除语义核验

- Heimdall 5.9.1 起，删除系统用户时在同一事务撤销平台角色、应用授权及其租户范围；删除租户用户时撤销目标租户下用户主键和无歧义历史 TenantUid 对应的应用角色与外部受管角色。任一步骤失败应回滚，历史脏数据不会在升级时自动删除。
- Heimdall 5.9.1 起，TOTP 永久删除会物理清除该用户当前和软删除的历史 TOTP 记录并失效缓存，不删除其他登录方式；停用仍保留绑定。租户路径必须约束租户并拒绝与其他用户重叠的历史标识。调用删除工具会影响当前绑定，不应把它当成仅清除历史记录的接口。
- 核对实际部署版本的 TOTP 删除工具、Service 与 Repository：软删除、停用和物理清除密钥材料不是同一语义。只有确认敏感字段已按契约清除后，才能声称材料已被永久删除；不要读取、输出或记录密钥内容。
- 审计已删除系统用户时，同时核对 `sys_user_role` 关联、登录凭据与主体会话。未软删的角色关系不等于账号当前可登录；反之，凭据失效也不能证明历史角色关联已清理。不要在未经明确授权时清除历史数据。
- MCP 工具的 `Destructive` 标记或描述不能替代对业务 Service、Repository 和持久化行为的核验。删除和恢复操作应覆盖上述边界的回归测试。

## 安全边界

- `McpIdentityBoundary` 是平台/租户身份的第一道边界。
- `McpAnyPermission` 声明允许的 Asgard 业务权限，`McpToolPermissionBoundary` 在工具发现和调用时都执行平台/租户范围及权限检查。
- `AllowedTools` 在 `tools/list` 隐藏不可用工具，并在 `tools/call` 再次拒绝。
- `AllowedPermissions` 在认证时裁剪标准 permissions claim，不能扩权。
- `/mcp` 端点策略在任何协议方法执行前拒绝非 Heimdall 管理应用签发的 OAuth Token；`tools/call` 过滤只作为纵深防护。
- 凭据生命周期工具调用 `McpCredentialPolicyBoundary`，禁止受限凭据委派更大的工具或权限集合。
- 按 ID 写入前读取目标资源并校验 TenantId 或使用既有 AccessGuard。
- 工具调用成功和失败都写入 `mcp.tool.invoked` 安全事件，包含工具、凭据、环境、结果、耗时和错误类型，不含秘密。
- 确认令牌短时、单次使用，绑定调用身份、工具名和载荷哈希。

## 协议原语

- Tools：完整平台与租户管理动作。
- Resources：平台总览、权限、租户详情、租户工作区、用户详情。
- Prompts：租户开通、用户离职、OIDC Client 安全审查、密钥轮换、安全事件调查、最小权限审查。
- Tasks：异步工具支持可选 Task augmentation；任务存储有 TTL、全局与会话容量限制。当前生产为单 Heimdall 容器，可使用内存 Store；扩展到多节点前必须改为共享持久化 Store。
- OAuth Protected Resource Metadata：远程客户端发现 Heimdall 授权服务器和 Bearer 方法。

## 验证清单

- 工具名称全部唯一。
- 每个工具类型带 `[Authorize]`。
- 每个非只读工具包含 `confirmationToken`。
- 平台/租户工具目录无交叉泄露。
- 租户按 ID 操作校验资源归属。
- AK/SK 停用、过期、CIDR、速率、并发和权限上限均有测试。
- OAuth 元数据路径和匿名发现契约有测试。
- `/mcp` 端点策略对 AK/SK、Heimdall OAuth、外部应用 OAuth 三类主体有授权测试。
- 受限凭据委派子集和越权拒绝有测试。
- 数据库迁移扩大 MCP 策略 JSON 字段且保持幂等。
- 后端先用 `dotnet build be/Test/Asgard.Heimdall.Tests/Asgard.Heimdall.Tests.csproj -c Release` 构建，再执行 `dotnet be/Test/Asgard.Heimdall.Tests/bin/Release/net10.0/Asgard.Heimdall.Tests.dll -noLogo -noColor`；核对实际测试数、失败数和跳过数。完整集成测试需配置隔离的 PostgreSQL/Redis，不能把零测试当通过。
- 前端 `npm run typecheck`、`npm run lint`、`npm run build` 全通过。
