# Heimdall MCP Contract

## 目录

1. [传输与认证](#传输与认证)
2. [凭据策略](#凭据策略)
3. [服务复用与实现地图](#服务复用与实现地图)
4. [工具契约](#工具契约)
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

客户端优先通过 MCP `tools/list`、`resources/list`、`prompts/list` 动态发现能力，不依赖静态工具清单。

## 凭据策略

`McpCredentialPolicyDto` 包含：

- `allowedTools`：工具白名单。
- `allowedPermissions`：用户实时业务权限的上限；认证时取交集。
- `allowedCidrs`：IPv4、IPv6、精确地址或 CIDR 来源白名单。
- `expiresAt`：UTC 失效时间。
- `environment`：`production`、`staging` 或 `development` 等审计标签。
- `rateLimitPerMinute`：凭据认证固定窗口额度。
- `maxConcurrentCalls`：工具并发上限。

空工具或权限列表表示“不额外收窄”，用于兼容已有凭据。停用、过期、来源不匹配或超限时认证失败。

当 MCP 凭据通过 `heimdall_*` 工具管理另一把凭据时，新策略的工具和权限必须是调用凭据边界的子集。调用凭据已有白名单时，新策略不能使用空列表，因为空列表会被解释为不额外收窄。

## 服务复用与实现地图

业务能力必须调用已有 `Services/IServices`：

- 平台：Tenant、SystemUser、Platform RBAC、Application、Manifest、Grant、Runtime/Security/Object Storage Settings、TOTP、Security Event、SIEM Export/Lifecycle、System Log、Trace。
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

## 安全边界

- `McpIdentityBoundary` 是平台/租户身份的第一道边界。
- `AsgardAuthAnyPermission` 等授权属性是业务权限边界。
- `AllowedTools` 在 `tools/list` 隐藏不可用工具，并在 `tools/call` 再次拒绝。
- `AllowedPermissions` 在认证时裁剪标准 permissions claim，不能扩权。
- `/mcp` 端点策略在任何协议方法执行前拒绝非 Heimdall 管理应用签发的 OAuth Token；`tools/call` 过滤只作为纵深防护。
- 凭据生命周期工具调用 `McpCredentialPolicyBoundary`，禁止受限凭据委派更大的工具或权限集合。
- 按 ID 写入前读取目标资源并校验 TenantId 或使用既有 AccessGuard。
- 工具调用成功和失败都写入 `mcp.tool.invoked` 安全事件，包含工具、凭据、环境、结果、耗时和错误类型，不含秘密。
- 确认令牌短时、单次使用，绑定 CredentialId、ToolName 和载荷哈希。

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
- 后端 `dotnet test -c Release` 全通过且 0 警告。
- 前端 `npm run typecheck`、`npm run lint`、`npm run build` 全通过。
