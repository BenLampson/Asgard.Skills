---
name: asgard-plugin-lifecycle
description: Asgard 插件生命周期 skill。Use when explaining or implementing host builder hooks, plugin lifecycle stages, PluginState transitions, service availability timing, startup order, shutdown behavior, or code that depends on lifecycle boundaries in Asgard.
---

# Asgard Plugin Lifecycle

## 作用

定义 Asgard 插件生命周期各阶段的职责、顺序、可用能力和禁忌。帮助理解宿主构建钩子顺序、插件状态转换、各阶段能做什么不能做什么。

结构与规则边界：

- 文件放置与项目结构见 `$asgard-plugin-structure`
- 代码实现必须继续遵守 `$asgard-dotnet-10-csharp-14`

## 什么时候使用

- **理解启动顺序** - 确定代码应该放在哪个阶段
- **解决 ServiceProvider 不可用问题** - 弄清楚为什么 `GetService` 抛出异常
- **重构插件代码** - 把代码移动到正确阶段
- **理解宿主钩子** - 理解 `BeforeConfigurationLoad`、`AfterConfigurationLoad` 等钩子何时执行
- **状态转换问题** - 理解 `PluginState` 各个值的含义

## 宿主构建阶段顺序

```
宿主构建入口
    ↓
BeforeConfigurationLoad → 用户钩子
    ↓
加载配置文件 + 环境变量 + 命令行
    ↓
AfterConfigurationLoad → 用户钩子
    ↓
加载并验证模块配置（database, caching, messaging, job 等）
    ↓
BeforeServiceRegistration → 用户钩子
    ↓
框架注册基础设施服务
    ↓
AfterServiceRegistration → 用户钩子
    ↓
构建 ASP.NET Core 主机
    ↓
框架初始化插件，调用 InitializeAsync（此时 ServiceProvider 可用）
    ↓
框架调用 ConfigureMiddlewareAsync，插件注册中间件
    ↓
AfterHostBuild → 用户钩子
    ↓
返回构建完成的 app
    ↓
app.RunAsync() → 启动
```

## 插件生命周期阶段

| 阶段 | 方法 | ServiceProvider 可用？ | 可以做什么 |
|------|------|-------------------|------------|
| **服务注册** | `ConfigureServicesAsync` | ❌ 不可用 | 只做服务注册（仓储、业务服务） |
| **初始化** | `InitializeAsync` | ✅ 可用 | 读取配置、分配资源、`GetService<T>()`、`GetAsgardContext()`、`CreateLogger()` |
| **中间件配置** | `ConfigureMiddlewareAsync` | ✅ 可用 | 注册中间件、映射端点 |
| **启动** | `StartAsync` | ✅ 可用 | 启动后台任务、建立连接、自动加载配置中定义的作业 |
| **停止** | `StopAsync` | ✅ 可用 | 停止接收新请求、停止后台任务 |
| **释放** | `DisposeAsync` | - | 释放资源 |

## 插件端点映射硬规则

在 `OnConfigureMiddlewareAsync` 中，如果只是注册 endpoint，应优先把 `context.App` 转为
`IEndpointRouteBuilder` 后调用 `Map...` 方法，例如 `MapMcp(...)`、`MapFallback(...)`
或其他 `Map...` 扩展。

不要在插件中调用 `context.App.UseEndpoints(...)`。Yggdrasil 的插件中间件扩展点位于
`UseAuthorization()` 之前；`UseEndpoints(...)` 会提前执行 endpoint，导致带
`[Authorize]` / `AsgardAuth` 元数据的 Controller 或 endpoint 报缺少授权中间件。

推荐模式：

```csharp
protected override Task OnConfigureMiddlewareAsync(
    IPluginMiddlewareConfigurationContext context,
    CancellationToken cancellationToken)
{
    if (context.App is IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapFallback(...);
    }

    return Task.CompletedTask;
}
```

## 宿主钩子可用位置

| 钩子 | 执行时机 | 使用场景 |
|------|----------|----------|
| `BeforeConfigurationLoad` | 配置加载之前 | 添加额外配置源 |
| `AfterConfigurationLoad` | 配置加载之后 | 根据配置动态调整服务 |
| `BeforeServiceRegistration` | 框架注册服务之前 | 覆盖框架默认注册 |
| `AfterServiceRegistration` | 框架注册服务之后 | 添加额外服务注册 |
| `ConfigureMiddleware` | 中间件配置阶段 | 配置 HTTP 管道 |
| `AfterHostBuild` | 宿主构建完成之后 | 最后调整、启动任务 |

## 插件状态转换流程

正常流程：
```
Unloaded → Loading → Loaded → Initializing → Initialized → Starting → Running
```

停止流程：
```
Running → Stopping → Stopped
```

卸载流程：
```
Stopped → Unloading → Unloaded
```

错误流程：
```
任意阶段 → Error
```

## 代码示例（各阶段正确写法）

```csharp
/// <summary>
/// {Summary}
/// </summary>
public class {PluginName} : PluginBase
{
    /// ConfigureServicesAsync 阶段
    /// 只做服务注册，不解析服务
    protected override Task OnConfigureServicesAsync(
        IPluginServiceConfigurationContext context,
        CancellationToken cancellationToken)
    {
        // ✅ 正确：这里只注册服务
        context.Services.AddRepositories(typeof({PluginName}).Assembly);
        context.Services.AddScoped<IMyService, MyService>();

        // ❌ 错误：不要在这里解析服务
        // var service = context.Services.BuildServiceProvider().GetService<IMyService>();

        return Task.CompletedTask;
    }

    /// InitializeAsync 阶段
    /// ServiceProvider 已可用，可以解析服务
    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        // ✅ 正确：这里可以解析服务
        var myService = GetService<IMyService>();
        var asgardContext = GetAsgardContext();
        var logger = CreateLogger();

        // 做初始化工作...

        return Task.CompletedTask;
    }

    /// ConfigureMiddlewareAsync 阶段
    /// 注册中间件和端点
    protected override Task OnConfigureMiddlewareAsync(
        IPluginMiddlewareConfigurationContext context,
        CancellationToken cancellationToken)
    {
        // ✅ 正确：这里可以注册中间件
        context.App.UseMyMiddleware();

        // ✅ 正确：这里只映射 endpoint，不调用 UseEndpoints(...)
        if (context.App is IEndpointRouteBuilder endpoints)
        {
            _ = endpoints.MapFallback(...);
        }

        return Task.CompletedTask;
    }

    /// StartAsync 阶段
    /// 启动后台任务
    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        // ✅ 正确：这里启动后台任务
        // 框架已经自动加载 plugin.yaml 中的作业

        return Task.CompletedTask;
    }

    /// StopAsync 阶段
    /// 停止后台任务
    protected override Task OnStopAsync(CancellationToken cancellationToken)
    {
        // ✅ 正确：这里停止后台任务

        return Task.CompletedTask;
    }
}
```

## 推荐做法

- 把服务注册放在 `ConfigureServicesAsync`，不要提前解析
- 需要解析服务时，放到 `InitializeAsync` 或之后阶段
- 注册中间件放到 `ConfigureMiddlewareAsync`
- 启动后台任务放到 `StartAsync`
- 理解 `PluginState` 反映框架生命周期，不要和业务状态混淆

## 不要这样做

❌ 不要在 `ConfigureServicesAsync` 解析 `ServiceProvider`，此时主机还没构建

❌ 不要在 `InitializeAsync` 之前调用 `GetService` / `GetAsgardContext` / `CreateLogger`，`PluginBase` 会检查阶段并抛出明确异常

❌ 不要把长期运行任务塞进 `ConfigureServicesAsync`，会阻塞构建

❌ 不要在插件中调用 `context.App.UseEndpoints(...)`；端点注册走 `IEndpointRouteBuilder.Map...`

❌ 不要把 `PluginState` 当作业务状态使用，它只反映框架生命周期位置

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `PluginState.cs` - 插件状态枚举
- `IPluginServiceConfigurationContext.cs` - 服务配置上下文接口
- `IPluginMiddlewareConfigurationContext.cs` - 中间件配置上下文接口

代码范本请参考 `templates/` 目录：
- `PluginLifecycleOrder.md.template` - 阶段顺序完整流程图
- `PhaseChecklist.cs.template` - 各阶段正确写法对照
