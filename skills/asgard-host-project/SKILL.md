---
name: asgard-host-project
description: "创建或修改 Asgard host/starter、Program.cs、YggdrasilHost 启动入口、宿主钩子和内建插件承载。插件目录布局用 asgard-plugin-structure。"
---

# Asgard Host Project

## 作用

编写 Asgard 宿主 / starter 项目时，负责选择正确入口、组织 `Program.cs` 结构、使用钩子扩展宿主行为。本 skill 定义了不同场景下的启动路径选择、宿主职责和钩子约定。

结构与规则边界：

- 宿主 / starter 启动结构看本 skill 自己的说明与 `templates/`
- 插件内部目录结构见 `$asgard-plugin-structure`
- 代码实现必须遵守 `$asgard-dotnet-10-csharp-14`

## 什么时候使用

- **创建新的 Asgard 宿主 / starter 项目** - 选择正确入口和启动路径
- **重构现有宿主 `Program.cs`** - 整理结构和钩子顺序
- **需要添加自定义中间件** - 在正确位置插入中间件配置
- **需要注册内建插件** - 使用 `UseBuiltInPlugin` 约定
- **需要理解宿主构建流程** - 按阶段理解构建顺序
- **需要判断 starter 和插件项目的引用关系** - 明确谁引用谁、谁负责启动

## 宿主 / starter 项目固定职责

- 负责启动路径选择
- 负责 `Program.cs`
- 负责中间件编排
- 负责 `YggdrasilHost.CreateBuilder(...)`
- 负责 `PluginWebAppDefaults.RunAsync<TPlugin>()`
- 负责加载 `app.yaml`
- 负责通过 `ProjectReference` 引用插件主体项目

端口等宿主监听配置写在 starter / host 项目的 `app.yaml` 中，并使用 `host.kestrel.endpoints.*.url` 形式，不要写成 `host.port`。

不要把插件内部目录结构也混进宿主结构说明里。

## 推荐 starter 结构

```text
{StarterProjectName}/
├── app.yaml
├── GlobalUsings.cs
├── Program.cs
└── {StarterProjectName}.csproj
```

对应关系：

- `Program.cs`
  启动入口、调试入口、参数解析入口
- `GlobalUsings.cs`
  starter 自己的全局 using
- `{StarterProjectName}.csproj`
  引用插件项目，承载启动期依赖
- `app.yaml`
  主运行配置

## 与插件项目的引用关系

- 推荐关系是：starter / host 项目引用插件主体项目
- 插件主体项目不反向依赖 starter
- `plugin.yaml` 默认位于插件主体项目
- `app.yaml` 默认由 starter / host 项目加载

## 启动路径选择

| 场景 | 推荐入口 | 说明 |
|------|----------|------|
| 快速验证单个插件 | starter 项目中的 `PluginWebAppDefaults.RunAsync<TPlugin>()` | 最短路径，适合单插件调试与启动 |
| 完整宿主 + 多个内建插件 | starter / host 项目中的 `YggdrasilHost.CreateBuilder()` | 完整构建器，支持钩子扩展 |
| 需要自定义配置钩子 | starter / host 项目中的 `YggdrasilHost.CreateBuilder()` | 通过钩子在各个阶段注入自定义逻辑 |
| 需要掌控中间件顺序 | starter / host 项目中的 `YggdrasilHost.CreateBuilder()` | 通过 `ConfigureMiddleware` 完全控制 |

## 构建钩子顺序

构建器提供多个钩子点，按以下顺序执行：

| 钩子 | 执行时机 | 使用场景 |
|------|----------|----------|
| `BeforeConfigurationLoad` | 配置加载之前 | 添加额外配置源、环境配置覆盖 |
| `AfterConfigurationLoad` | 配置加载之后 | 根据配置动态调整服务 |
| `BeforeServiceRegistration` | 框架服务注册之前 | 注册自定义服务覆盖框架默认 |
| `AfterServiceRegistration` | 框架服务注册之后 | 添加额外服务注册 |
| `ConfigureMiddleware` | 中间件管道构建 | 配置中间件顺序 |
| `AfterHostBuild` | 宿主构建完成之后 | 最后调整，注册启动任务 |

## 什么时候需要手写 `UseAuthentication()` / `UseAuthorization()`

| 场景 | 是否通常需要手写 | 原因与建议 |
|------|------------------|------------|
| 使用 Yggdrasil 默认链路（`YggdrasilHost.CreateBuilder(...)`） | 否 | 默认管道已按顺序托底：按需 `UseAuthentication()`（受 `host.auth.enabled` 控制）+ 统一 `UseAuthorization()` |
| 使用 starter 中的 `PluginWebAppDefaults.RunAsync<TPlugin>()` 且走宿主默认认证配置 | 否 | 与宿主默认链路一致，通常不需要重复补线 |
| 完全自定义宿主管道或旁路宿主（不走默认配置链路） | 是 | 需要你自己显式保证认证与授权中间件接入和顺序 |
| 宿主关闭默认 JWT（`host.auth.enabled: false`）但由插件/外部方案提供认证主体 | 视实现而定 | 若外部方案已接入认证中间件可不重复；若未接入，则需显式补 `UseAuthentication()`，`UseAuthorization()` 仍必须存在 |

判定原则：

- 不要把“示例里出现了中间件”理解成“所有项目都必须手写一遍”
- 只有在你脱离默认链路、或默认链路无法覆盖你的认证实现时，才需要显式补线

### 版本兼容提醒：`PluginWebAppDefaults` 授权托底

从修复 `PluginWebAppDefaults.RunAsync<TPlugin>()` 与 Yggdrasil 默认链路不一致的版本开始，快速入口也应由框架统一托底 `UseAuthorization()`，并在 `host.auth.enabled: true` 时统一接入宿主默认 `UseAuthentication()`。

升级后的迁移原则：

- 现有 starter 里已经手写 `app.UseAuthentication().UseAuthorization()` 的项目可以继续运行，不需要为了升级立即改代码。
- 新项目和新模板不要再默认手写认证授权中间件，优先保持 `await PluginWebAppDefaults.RunAsync<TPlugin>(configPath);`。
- 维护旧项目时，可以在确认没有插件自定义认证链路依赖该回调后，逐步删除 starter 里的重复 `UseAuthentication()` / `UseAuthorization()`。
- 重复调用通常不会导致应用启动失败，但可能让认证或授权处理重复执行；不要把“重复也能跑”当成推荐结构。

## 推荐代码结构

### starter 最简启动（单个内建插件）

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<{PluginName}>("app.yaml");
```

如果插件项目自己注册了认证服务，并且不依赖 `host.auth` 提供默认认证主体，可以只在回调里补充该认证中间件；`UseAuthorization()` 仍由宿主默认链路统一兜底：

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<{PluginName}>(
    "app.yaml",
    app =>
    {
        _ = app.UseAuthentication();
    });
```

### starter 完整启动（带钩子）

```csharp
using Asgard.Yggdrasil.AspNetCore;

var builder = YggdrasilHost.CreateBuilder("app.yaml")
    .BeforeConfigurationLoad(hostBuilder =>
    {
        // 在配置加载之前做一些事情
        {BeforeConfigurationLoad}
    })
    .BeforeServiceRegistration(services =>
    {
        // 在框架服务注册之前添加额外服务
        {BeforeServiceRegistration}
    })
    .UseBuiltInPlugin<{PluginName}>()
    .ConfigureMiddleware(app =>
    {
        _ = app.UseAsgardExceptionHandler()
            .UseHttpsRedirection();
        {ExtraMiddleware}
    });

var app = builder.Build();
{AfterBuild}
await app.RunAsync();
```

## 内建插件注册

| 方法 | 使用场景 |
|------|----------|
| `UseBuiltInPlugin<TPlugin>()` | 注册单个内建插件 |
| `UseBuiltInPluginsFromAssembly(assembly)` | 扫描程序集注册所有内建插件 |
| `UseEntryAssemblyPlugins()` | 扫描入口程序集注册所有内建插件 |

## 构建流程

```
1. BeforeConfigurationLoad → 调用你的钩子
2. 加载 YAML 配置文件
3. AfterConfigurationLoad → 调用你的钩子
4. BeforeServiceRegistration → 调用你的钩子
5. 框架注册基础设施服务（缓存、数据库、消息队列、作业调度）
6. AfterServiceRegistration → 调用你的钩子
7. 构建 ASP.NET Core host
8. 初始化插件管理器
9. ConfigureMiddleware → 配置中间件管道
10. AfterHostBuild → 调用你的钩子
11. 返回构建完成的 WebApplication
```

## 构建失败与资源所有权

目标包含 Asgard 6.0.1 基础设施生命周期修复（2026-10-08 核对；发布状态另行确认）时，宿主统一拥有构建前创建的缓存、MQ、作业调度、追踪和引导日志资源；DI 中注入的相关实例是借用。不要在业务服务或插件里分别 Dispose 宿主提供的管理器/连接/调度器。

- 宿主停止时先关闭作业、清理插件，期间 MQ/缓存/追踪仍可用；随后释放基础设施，最后排空追踪并关闭引导日志
- 构建失败会回滚已创建资源；构建后即使未 Start，Dispose 也会清理；重复停止/释放复用同一清理过程
- 清理失败不阻止继续释放其他资源，错误汇总返回；不能吞掉它们然后宣称全部关闭成功
- 同一 builder 重复 Build 仍受支持，每次构建使用独立资源所有者，不要给用户新增“一次性 builder”限制
- 由调用者自行创建的外部资源仍按其所有权契约处理；不要把宿主接管推及所有任意 DI 实例

读取 `$asgard-plugin-lifecycle` 的关闭与重载契约，并核对 Asgard 的 `AsgardRuntimeLifetime.cs` 与 `YggdrasilHostBuilder*` 当前实现。旧参考源码未包含补丁的部分不能用于复制生命周期接线。

## 推荐做法

- `Program.cs` 默认放在 starter / host 项目，而不是插件主体项目
- 主配置文件通常由 starter / host 项目加载
- 按照钩子执行顺序理解，不要在错误阶段做错误的事情
- 对于单个插件调试，优先使用 `PluginWebAppDefaults.RunAsync<TPlugin>()` 最短路径
- 当认证/授权由插件自身注册时，在 `RunAsync(..., configure)` 中显式补齐 `UseAuthentication()` 与 `UseAuthorization()`
- 需要内建多个插件时，使用 `UseEntryAssemblyPlugins()` 一次性注册所有
- 需要添加自定义中间件时，通过 `ConfigureMiddleware` 回调添加

## 不要这样做

❌ 不要把宿主 / starter 结构和插件内部业务目录写成同一套结构

❌ 不要把插件主体项目写成宿主入口默认位置

❌ 不要跳过 `YggdrasilHost` 直接手搓一整套宿主，除非用户明确要求脱离框架

❌ 不要把配置加载、插件注册等逻辑散落在多个文件，保持 `Program.cs` 入口清晰

❌ 不要颠倒钩子顺序，比如在 `BeforeConfigurationLoad` 中使用已加载的配置

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `YggdrasilHost.cs` - 静态入口
- `YggdrasilHostBuilder.cs` - 构建器核心
- `PluginWebAppDefaults.cs` - 默认快捷入口

源码锚点：

- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Configurator.cs` - 默认中间件顺序与统一 `UseAuthorization()`
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Services.cs` - `host.auth.enabled` 与默认 JWT 注册
- `Common/Asgard.AspNetCore.Core/ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 授权策略注册
- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - `AsgardAuth` 特性绑定策略
- `Common/Asgard.Abstractions.AspNetCore/Host/AuthOptions.cs` - `host.auth.enabled` 注释与语义

代码范本请参考 `templates/` 目录：
- `Program-Minimal.cs.template` - starter 最简入口
- `Program-Full.cs.template` - starter 完整入口
- `Program-WithHooks.cs.template` - starter 带钩子入口

## 6.0.2：作业延迟到托管启动

以下适用于 Asgard 6.0.2 运行时优化源码；发布状态需另行核对，不表示 NuGet 已完成发布。维护旧包时先确认包含对应实现，不把这些行为追溯到 6.0.1。

宿主 Build/BuildAsync 只通过 `JobManager.PrepareAsync` 准备调度器并注册配置作业，随后设置最终 DI 提供者，完成插件初始化/启动钩子及中间件配置。宿主 `StartAsync` 的运行时托管服务才调用管理器初始化并真正启动 Quartz，传递宿主取消令牌；仅 Build 后调度器 `IsStarted=false`，`StartNow` 不能提前执行。排队注册错误或启动取消传播并使用清理路径；未 Start 的宿主释放仍清理准备资源。

独立 `JobManager.InitializeAsync` 保持立即准备并启动，不能把宿主延迟启动规则套用到独立用法。配置限制、每次触发的 DI 作用域与作业资源所有权见 `$asgard-job-scheduling`。本目录旧源码快照仍是历史参考，6.0.2 接线以目标 `YggdrasilHostBuilder`、`AsgardRuntimeHostedService`、`JobManager` 为准。
