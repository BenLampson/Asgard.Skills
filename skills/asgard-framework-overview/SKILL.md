---
name: asgard-framework-overview
description: Asgard 框架总览与路由 skill。Use when working with the Asgard framework but the request spans multiple modules, the correct entry point is unclear, or another AI needs a high-level map before choosing host, configuration, api, plugin, context, lifecycle, repository, or infrastructure skills.
---

# Asgard Framework Overview

## 先做路由判断

- 先读取 `../../doc/01-框架概览.md`、`../../doc/02-快速开始.md`、`../../doc/09-源码参考索引.md`。
- 先判断用户问题属于哪个模块，再决定是否继续读取专项 skill 或源码。
- 先记住推荐入口：`YggdrasilHost.CreateBuilder(...)`、`PluginWebAppDefaults.RunAsync<TPlugin>()`、`UseBuiltInPlugin<TPlugin>()`、`BaseController`、`AbsAsgardContext`。
- 如果走 Yggdrasil 默认链路，通常不需要手写 `UseAuthorization()`；只有完全自定义或旁路默认链路时，才需要显式补齐认证授权中间件。

## 能力矩阵（谁负责什么）

| 能力 | 默认责任方 | 关键结论 |
|------|------------|----------|
| 认证主体构建（host.auth / 插件自定义） | 宿主 `host.auth` 或插件/外部方案 | `host.auth.enabled: true` 时宿主管默认 JWT；`false` 时可由插件/外部方案接管 |
| 身份快照建立（UseAsgardTenant + IdentityContext） | Asgard 上下文与租户中间件链路 | 业务统一从 `AsgardContext.IdentityContext` 读取身份，不建议自行解析 claim |
| 授权执行（UseAuthorization + AsgardAuth policy） | Asgard 授权策略 + ASP.NET Core 授权中间件 | `AsgardAuth` policy 由框架注册，Yggdrasil 默认链路统一执行 `UseAuthorization()` |

## 按问题选择专项 skill

- 宿主项目与启动编排：使用 `$asgard-host-project`。
- 项目结构、基础文件、目录分层：使用 `$asgard-plugin-structure`。
- 配置体系、`app.yaml`、`plugin.yaml`、`ConfigPath`：使用 `$asgard-configuration`。
- `host.staticFiles`、`host.auth`、`host.swagger`、限流、健康检查：使用 `$asgard-host-features`。
- Web API、控制器、统一响应：使用 `$asgard-api-development`。
- 插件实现、插件约定、内建插件与外部插件：使用 `$asgard-plugin-development`。
- 宿主钩子、插件阶段、状态机：使用 `$asgard-plugin-lifecycle`。
- `AbsAsgardContext` 与公共能力获取：使用 `$asgard-context-usage`。
- `AbsAsgardUserInfo`、`IAsgardIdentityContext`、IDP claim 设计、测试身份构造：使用 `$asgard-identity-userinfo`。
- 基类、响应模型、字段语义、什么时候继承：使用 `$asgard-base-types`。
- 仓储扫描、服务注册、约定装配：使用 `$asgard-repository-service-registration`。
- 缓存、数据库、消息、作业、安全：分别使用 `$asgard-cache`、`$asgard-database`、`$asgard-messaging`、`$asgard-job-scheduling`、`$asgard-security`。

## 保持全局共识

- 优先推荐“内建插件 + Asgard 宿主”路径，不要默认从零拼一套 ASP.NET Core 架构。
- 优先把框架能力从 `AbsAsgardContext` 获取；确实需要更底层控制时再注入具体接口。
- 优先沿用项目根目录 `app.yaml` 作为主配置入口；插件单独能力放 `plugin.yaml`。
- 优先让控制器薄、服务显式、仓储只做数据访问、插件承载模块边界。
- 生成代码时保持与 `$asgard-dotnet-10-csharp-14` 一致的仓库级规则。

## 不要这样做

- 不要在总览 skill 里塞入所有模块细节；遇到具体功能时切到对应专项 skill。
- 不要绕过现有文档与源码入口去发明新抽象。
- 不要假设所有模块都已启用；Asgard 大量能力都是配置驱动且可空的。

## 源码锚点

以下锚点用于总览问题快速落到真实实现：

- `Common/Asgard.AspNetCore.Core/ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 与 `AsgardAuth` policy 注册
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Services.cs` - `host.auth.enabled` 与默认 JWT 服务注册
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Configurator.cs` - 默认中间件顺序与 `UseAuthorization()`
- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - 授权特性与策略绑定
- `Common/Asgard.Abstractions.AspNetCore/Host/AuthOptions.cs` - 认证配置语义
