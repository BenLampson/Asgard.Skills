---
name: asgard-framework-overview
description: "为跨模块或入口不明确的 Asgard 框架任务选择专项 skill，说明宿主、插件和基础设施的职责。已明确模块的任务直接使用对应 skill。"
---

# Asgard Framework Overview

## 先做路由判断

- 先读取本 skill 的 `references/framework-architecture.md`；需要核对实现时，优先读取本 skill `references/` 中与目标版本匹配的源码拷贝；历史快照不匹配时以可用的目标源码为准。
- 先判断用户问题属于哪个模块，再决定是否继续读取专项 skill 或源码。
- 先区分“插件入口类”和“启动入口 Program.cs”。
- 先判断当前仓库是单项目快速验证，还是“插件实现 + starter 启动器”双项目分离。
- 先记住推荐入口：`YggdrasilHost.CreateBuilder(...)`、`PluginWebAppDefaults.RunAsync<TPlugin>()`、`UseBuiltInPlugin<TPlugin>()`、`BaseController`、`AbsAsgardContext`。
- 如果走 Yggdrasil 默认链路，通常不需要手写 `UseAuthorization()`；只有完全自定义或旁路默认链路时，才需要显式补齐认证授权中间件。

## 版本判断

- 先读取目标项目的包引用、中央包版本或 `Directory.Build.props`，按实际目标版本选择 API；不要根据目录名推断版本。
- 缓存 skill 与新缓存模板面向 5.3+；旧缓存源码快照只服务旧版本维护。升级任务按 `$asgard-cache` 迁移，不混用两代接口。
- 不把某个固定版本称作“最新”。发包或升级时核对目标源码版本；缺少事实时明确版本假设，不擅自升级项目。

## 推荐项目组织方式

Asgard 当前更推荐：

- 插件主体项目负责插件实现
- starter / host 项目负责启动与调试承载
- 正式开发优先采用双项目分离
- 插件主体内部优先顶层按 Asgard 标准层组织，复杂业务再在层内按模块分子目录

补充判断：

- 单项目结构适合快速验证
- 不要把快速验证示例当成唯一标准结构
- 不要默认把启动入口 `Program.cs` 放进插件主体项目
- 当仓库已经采用分离结构时，应优先尊重现有结构
- 不要默认把业务模块作为第一层目录后再重复 `Models / Domains / Services` 作为正式推荐结构

## 能力矩阵（谁负责什么）

| 能力 | 默认责任方 | 关键结论 |
|------|------------|----------|
| 启动入口 `Program.cs` | starter / host 项目 | 默认承载 `PluginWebAppDefaults.RunAsync<TPlugin>()` 或 `YggdrasilHost.CreateBuilder(...)` |
| 插件入口类 `PluginBase` | 插件主体项目 | 负责插件元数据、服务装配与生命周期 |
| `plugin.yaml` | 插件主体项目 | 插件清单与插件级元数据 |
| `app.yaml` | starter / host 项目 | 运行配置入口；也可由插件项目输出资源承载，但应明确由启动方加载 |
| 认证主体构建（host.auth / 插件自定义） | 宿主 `host.auth` 或插件/外部方案 | `host.auth.enabled: true` 时宿主管默认 JWT；`false` 时可由插件/外部方案接管 |
| 身份快照建立（UseAsgardTenant + IdentityContext） | Asgard 上下文与租户中间件链路 | 业务统一从 `AsgardContext.IdentityContext` 读取身份，不建议自行解析 claim |
| 授权执行（UseAuthorization + AsgardAuth policy） | Asgard 授权策略 + ASP.NET Core 授权中间件 | `AsgardAuth` policy 由框架注册，Yggdrasil 默认链路统一执行 `UseAuthorization()` |

## 按问题选择专项 skill

- 宿主项目与启动编排：使用 `$asgard-host-project`。
- 项目结构、基础文件、目录分层：使用 `$asgard-plugin-structure`。
- 配置体系、`app.yaml`、`plugin.yaml`、`ConfigPath`：使用 `$asgard-configuration`。
- `host.staticFiles`、`host.auth`、`host.swagger`、限流、健康检查：使用 `$asgard-host-features`。
- Web 登录流、OIDC / PKCE、IDP 对接、token 契约与前后端认证协作：使用 `$identity-integration`。
- 管理后台前端、Heimdall 风格页面、TsGen 客户端消费、Umi/Ant Design Pro 页面调用链：使用 `$asgard-admin-frontend`。
- Web API、控制器、统一响应：使用 `$asgard-api-development`。
- `AsgardAuth`、授权 DSL、`token_type` / 角色 / 权限 / metadata 授权：使用 `$asgard-auth-authorization`。
- 插件实现、插件约定、内建插件与外部插件：使用 `$asgard-plugin-development`。
- 宿主钩子、插件阶段、状态机：使用 `$asgard-plugin-lifecycle`。
- `AbsAsgardContext` 与公共能力获取：使用 `$asgard-context-usage`。
- 请求追踪、运行时链路说明、`AsgardContext.Trace`：使用 `$asgard-tracing-observability`。
- `AbsAsgardUserInfo`、`IAsgardIdentityContext`、IDP claim 设计、测试身份构造：使用 `$asgard-identity-userinfo`。
- 基类、响应模型、字段语义、什么时候继承：使用 `$asgard-base-types`。
- 仓储扫描、服务注册、约定装配：使用 `$asgard-repository-service-registration`。
- 缓存、分布式锁、数据库、消息、作业、安全：分别使用 `$asgard-cache`、`$asgard-distributed-lock`、`$asgard-database`、`$asgard-messaging`、`$asgard-job-scheduling`、`$asgard-security`。

- Heimdall 微服务身份与撤销同步：使用 `$heimdall-service-integration`。
- Heimdall 应用域权限与 Tenant 绑定：使用 `$heimdall-application-rbac`。
- Heimdall MCP 管理能力：使用 `$heimdall-mcp-management`。
- .NET 单元测试与 xUnit v3：使用 `$dotnet-unit-testing`。

## 保持全局共识

- 优先推荐“内建插件 + Asgard 宿主”路径，不要默认从零拼一套 ASP.NET Core 架构。
- 优先推荐“插件主体项目 + starter 项目分离”，不要默认宣传单项目承载全部职责。
- 优先让插件主体顶层保持 `Controllers / Mapper / Models / Domains / Services / Infrastructure` 等标准层；多业务模块放到这些标准层内部继续分组。
- 优先把框架能力从 `AbsAsgardContext` 获取；确实需要更底层控制时再注入具体接口。
- 优先沿用 starter / host 加载 `app.yaml`，插件主体维护 `plugin.yaml`。
- 优先让控制器薄、服务显式、仓储只做数据访问、插件承载模块边界。
- 生成代码时保持与 `$asgard-dotnet-10-csharp-14` 一致的仓库级规则。

## 不要这样做

- 不要在总览 skill 里塞入所有模块细节；遇到具体功能时切到对应专项 skill。
- 不要绕过现有文档与源码入口去发明新抽象。
- 不要假设所有模块都已启用；Asgard 大量能力都是配置驱动且可空的。
- 不要把“快速验证示例”表述成唯一推荐结构。

## 源码锚点

完整源码拷贝请参考 `references/` 目录：

- `ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 与 `AsgardAuth` policy 注册
- `YggdrasilHostBuilder.Services.cs` - `host.auth.enabled` 与默认 JWT 服务注册
- `YggdrasilHostBuilder.Configurator.cs` - 默认中间件顺序与 `UseAuthorization()`
- `AsgardAuthAttributes.cs` - 授权特性与策略绑定
- `AuthOptions.cs` - 认证配置语义
