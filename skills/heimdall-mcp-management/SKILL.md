---
name: heimdall-mcp-management
description: Heimdall MCP 管理与集成 skill。Use when building, extending, reviewing, testing, or consuming Heimdall's `/mcp` Streamable HTTP server, management tools, Resources, Prompts, Tasks, OAuth Bearer or AK/SK authentication, credential policies, tenant boundaries, write confirmation, audit, rate limits, or MCP administration UI.
---

# Heimdall MCP Management

## 作用

把 Heimdall 已有平台与租户管理 Service 安全暴露为 MCP，并指导客户端正确发现、认证和调用。详细协议与实现地图见 `references/mcp-contract.md`。

## 先判断任务类型

- 开发或审查 MCP 工具：读取 `references/mcp-contract.md` 的“服务复用”和“工具契约”。
- 接入远程 MCP 客户端：读取“传输与认证”。
- 配置 MCP 凭据：读取“凭据策略”。
- 排查越权、跨租户、重复写入或审计：读取“安全边界”。
- 发布 Heimdall MCP：读取“验证清单”，并同时使用 `$asgard-backend-guard`、`$asgard-admin-frontend`。

## 核心原则

1. 复用现有业务 Service。MCP 工具只做参数绑定、身份边界、权限、确认和结果包装，不复制 Controller 或 Service 业务逻辑。
2. 只为 MCP 特有治理增加 Service，例如工具目录、凭据策略、确认令牌、限流、并发和审计。
3. 租户工具从认证身份取得 TenantId；不要接受可切换租户的调用参数。
4. 平台工具显式校验平台身份；应用、角色、Client、Provider 等资源继续调用已有 AccessGuard。
5. 每个非只读工具必须使用 `McpWriteGuard` 二阶段确认，并把令牌绑定凭据、工具名和完整载荷。
6. 每个工具类型使用 `[Authorize]`，每个业务工具继续声明对应 `AsgardAuth*` 权限。
7. 工具名是稳定公共契约；使用 `platform_`、`tenant_` 或 `heimdall_` 前缀，禁止重复和静默改名。
8. `/mcp` 必须使用端点级授权策略：AK/SK 凭据可访问，OAuth Bearer 必须由 Heimdall 内置管理应用签发；不能只在 `tools/call` 过滤。
9. MCP 凭据创建或更新另一把凭据时，委派工具和权限必须是当前凭据边界的子集，禁止通过新 AK/SK 自提权。

## 开发流程

1. 找到对应 Controller 使用的 Service、DTO、VO 和 AccessGuard。
2. 把只读能力放入对应平台或租户工具类；分页统一返回 `McpPageResult<T>`。
3. 把写能力标记正确的 `ReadOnly`、`Destructive`、`Idempotent`，并接入二阶段确认。
4. 对按 ID 操作先读取资源并校验归属，禁止只信任调用方 DTO 的 TenantId。
5. 需要长时执行时保持异步签名，让 MCP Tasks 可选增强接管；不要自行发明轮询协议。
6. 新工具自动进入程序集扫描和工具目录；不要维护前端硬编码工具清单。
7. 补充契约测试，再运行后端 Release 测试和前端 typecheck、lint、build。

## 不要这样做

- 不要为已有业务接口再创建平行业务 Service。
- 不要在 MCP 工具中直接操作 Repository、Entity 或 FreeSql。
- 不要把平台管理员权限自动赋给 MCP 凭据。
- 不要让 `AllowedPermissions` 替代用户实时权限；它只能作为权限上限并取交集。
- 不要把空工具/权限列表解释为拒绝全部；当前兼容语义是“不额外收窄”。
- 不要让写工具跳过预览与确认，即使 SDK 标记了 `Destructive=false`。
- 不要只保护 Tools；Resources、Prompts、Tasks 和未来协议方法必须共同受 `/mcp` 端点授权策略保护。
- 不要让受限 MCP 凭据创建空白名单或包含额外工具/权限的新凭据；空列表在当前语义下代表“不额外收窄”。
- 不要返回或记录 SK、Client Secret、SCIM Token、Webhook Secret、私钥或完整敏感请求头。
- 不要手改 TsGen 的 `controller/`、`models/`、`common/` 生成目录。

## 协同 skill

- C# 规则与后端复查：`$asgard-dotnet-10-csharp-14`、`$asgard-backend-guard`
- Controller 与授权：`$asgard-api-development`、`$asgard-auth-authorization`
- 身份/OAuth 集成：`$identity-integration`、`$asgard-identity-userinfo`
- 管理前端：`$asgard-admin-frontend`
- Heimdall 应用 RBAC：`$heimdall-application-rbac`
- 生产发布：`$heimdall-production-release`
