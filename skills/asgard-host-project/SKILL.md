---
name: asgard-host-project
description: Asgard ASP.NET Host 项目编写 skill。Use when creating, refactoring, or explaining an Asgard host project, including startup entry selection, YggdrasilHost usage, Program.cs layout, middleware registration, built-in plugin hosting, and host-level project structure.
---

# Asgard Host Project

## 作用

编写 Asgard 宿主项目时，负责选择正确入口、组织 Program.cs 结构、使用钩子扩展宿主行为。本 skill 定义了不同场景下的启动路径选择和钩子约定。

结构与规则边界：

- 宿主或插件项目的目录结构见 `$asgard-plugin-structure`
- 代码实现必须遵守 `$asgard-dotnet-10-csharp-14`

## 什么时候使用

- **创建新的 Asgard 宿主项目** - 选择正确入口和启动路径
- **重构现有宿主 Program.cs** - 整理结构和钩子顺序
- **需要添加自定义中间件** - 在正确位置插入中间件配置
- **需要注册内建插件** - 使用 `UseBuiltInPlugin` 约定
- **需要理解宿主构建流程** - 按阶段理解构建顺序

## 启动路径选择

| 场景 | 推荐入口 | 说明 |
|------|----------|------|
| 快速验证单个插件 | `PluginWebAppDefaults.RunAsync<TPlugin>()` | 最短路径，适合无额外安全中间件需求的场景 |
| 完整宿主 + 多个内建插件 | `YggdrasilHost.CreateBuilder()` | 完整构建器，支持钩子扩展 |
| 需要自定义配置钩子 | `YggdrasilHost.CreateBuilder()` | 通过钩子在各个阶段注入自定义逻辑 |
| 需要掌控中间件顺序 | `YggdrasilHost.CreateBuilder()` | 通过 `ConfigureMiddleware` 完全控制 |

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
| 使用 `PluginWebAppDefaults.RunAsync<TPlugin>()` 且走宿主默认认证配置 | 否 | 与宿主默认链路一致，通常不需要重复补线 |
| 完全自定义宿主管道或旁路宿主（不走默认配置链路） | 是 | 需要你自己显式保证认证与授权中间件接入和顺序 |
| 宿主关闭默认 JWT（`host.auth.enabled: false`）但由插件/外部方案提供认证主体 | 视实现而定 | 若外部方案已接入认证中间件可不重复；若未接入，则需显式补 `UseAuthentication()`，`UseAuthorization()` 仍必须存在 |

判定原则：

- 不要把“示例里出现了中间件”理解成“所有项目都必须手写一遍”
- 只有在你脱离默认链路、或默认链路无法覆盖你的认证实现时，才需要显式补线

## 推荐代码结构

### 最简启动（单个内建插件）

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<{PluginName}>("app.yaml");
```

如果插件项目自己注册了认证/授权服务，而不是依赖 `host.auth`：

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<{PluginName}>(
    "app.yaml",
    app =>
    {
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
    });
```

### 完整启动（带钩子）

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

## 代码示例

### 最简入口（推荐快速开发）

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<MyPlugin>("app.yaml");
```

如果插件内部自己调用了 `AddAuthentication()` / `AddAuthorization()`，则最简入口也要补充安全中间件：

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<MyPlugin>(
    "app.yaml",
    app =>
    {
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
    });
```

### 完整控制

```csharp
using Asgard.Yggdrasil.AspNetCore;

var builder = YggdrasilHost.CreateBuilder("app.yaml")
    .UseBuiltInPlugin<MyPlugin>()
    .ConfigureMiddleware(app =>
    {
        _ = app.UseAsgardExceptionHandler()
            .UseHttpsRedirection();
    });

var app = builder.Build();
await app.RunAsync();
```

### 带额外服务注册

```csharp
using Asgard.Yggdrasil.AspNetCore;

var builder = YggdrasilHost.CreateBuilder("app.yaml")
    .BeforeServiceRegistration(services =>
    {
        // 添加你的额外服务
        services.AddScoped<IMyCustomService, MyCustomService>();
    })
    .UseBuiltInPlugin<MyPlugin>()
    .ConfigureMiddleware(app =>
    {
        _ = app.UseAsgardExceptionHandler()
            .UseHttpsRedirection();
    });

var app = builder.Build();
await app.RunAsync();
```

## 推荐做法

- 主配置文件固定放在项目根目录 `app.yaml`
- 按照钩子执行顺序理解，不要在错误阶段做错误的事情
- 对于单个插件项目，优先使用 `PluginWebAppDefaults.RunAsync<TPlugin>()` 最短路径
- 当认证/授权由插件自身注册时，在 `RunAsync(..., configure)` 中显式补齐 `UseAuthentication()` 与 `UseAuthorization()`
- 需要内建多个插件时，使用 `UseEntryAssemblyPlugins()` 一次性注册所有
- 需要添加自定义中间件时，通过 `ConfigureMiddleware` 回调添加

## 不要这样做

❌ 不要跳过 `YggdrasilHost` 直接手搓一整套宿主，除非用户明确要求脱离框架

❌ 不要把配置加载、插件注册等逻辑散落在多个文件，保持 Program.cs 入口清晰

❌ 不要在需要快速验证时放弃 `PluginWebAppDefaults.RunAsync<TPlugin>()` 去写完整构建器

❌ 不要颠倒钩子顺序，比如在 `BeforeConfigurationLoad` 中使用已加载的配置

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `YggdrasilHost.cs` - 静态入口
- `YggdrasilHostBuilder.cs` - 构建器核心
- `PluginWebAppDefaults.cs` - 默认快捷入口

## 源码锚点

以下锚点用于快速核对“默认链路是否已托底”：

- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Configurator.cs` - 默认中间件顺序与统一 `UseAuthorization()`
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Services.cs` - `host.auth.enabled` 与默认 JWT 注册
- `Common/Asgard.AspNetCore.Core/ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 授权策略注册
- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - `AsgardAuth` 特性绑定策略
- `Common/Asgard.Abstractions.AspNetCore/Host/AuthOptions.cs` - `host.auth.enabled` 注释与语义

代码范本请参考 `templates/` 目录：
- `Program-Minimal.cs.template` - 最简入口
- `Program-Full.cs.template` - 完整入口
- `Program-WithHooks.cs.template` - 带钩子入口
