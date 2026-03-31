---
name: asgard-plugin-development
description: Asgard 插件开发 skill。Use when implementing, registering, refactoring, or explaining Asgard plugins, including PluginBase, IPlugin, built-in plugins, external plugins, plugin.yaml, plugin conventions, and plugin-level service registration.
---

# Asgard Plugin Development

## 作用

定义 Asgard 插件开发的约定和最佳实践。插件是 Asgard 的核心扩展机制，通过插件可以独立部署业务功能，不需要重新编译宿主。本 skill 帮助你选择正确插件形态、实现插件、注册服务、处理生命周期。

## 什么时候使用

- **创建新插件** - 从零开始编写新插件
- **重构现有插件** - 整理插件代码结构
- **理解插件约定** - 理解各阶段职责和可用能力
- **添加插件配置** - 在 `plugin.yaml` 中定义配置
- **添加自动加载作业** - 自动从配置注册定时任务

## 插件形态选择

| 形态 | 使用场景 | 推荐入口 |
|------|----------|----------|
| **内建插件** | 和宿主一起编译，快速开发 | `PluginWebAppDefaults.RunAsync<TPlugin>()` / `UseBuiltInPlugin<TPlugin>()` |
| **外部插件** | 独立部署，热插拔，不需要重新编译宿主 | 文件系统扫描，插件放在 `plugins/` 目录 |

**推荐**：开发阶段优先使用内建插件，需要独立部署时再改为外部插件。

## 开发约定

| 阶段 | 方法 | 可做什么 | 不可做什么 |
|------|------|----------|------------|
| `ConfigureServicesAsync` | 注册服务、仓储、配置绑定 | 不能获取 `ServiceProvider`（还没构建） |
| `InitializeAsync` | 读取配置、分配资源 | 可以用 `GetService<T>()`、`GetAsgardContext()` |
| `ConfigureMiddlewareAsync` | 注册中间件、端点映射 | 可以获取 `ServiceProvider` |
| `StartAsync` | 启动后台任务、建立连接 | - |
| `StopAsync` | 停止接收请求、停止后台任务 | - |

## 核心继承选择

- 优先继承 `PluginBase`，不要从零实现 `IPlugin`
- `PluginBase` 已经提供：状态管理、生命周期守卫、便捷方法（`GetService`、`GetAsgardContext`、`CreateLogger`）、自动加载作业

## 必须实现的抽象属性

| 属性 | 说明 | 示例 |
|------|------|------|
| `Id` | 插件唯一标识 | `public override string Id => "my-plugin";` |
| `Name` | 插件显示名称 | `public override string Name => "我的插件";` |
| `Version` | 插件版本 | `public override Version Version => new(1, 0, 0);` |
| `Description` | 插件描述（可选）| `public override string Description => "这是我的插件";` |
| `Dependencies` | 依赖的其他插件 ID 列表（可选）| `public override IReadOnlyList<string> Dependencies => ["another-plugin"];` |

## 便捷方法（继承 `PluginBase` 后可用）

| 方法 | 说明 |
|------|------|
| `GetService<T>()` | 获取必需服务（框架检查阶段，不安全调用会报错） |
| `GetOptionalService<T>()` | 获取可选服务，不存在返回 null |
| `CreateLogger()` | 创建当前插件类型的日志器 |
| `GetAsgardContext()` | 获取 Asgard 上下文（包含缓存、消息、作业等公共能力） |

## 代码示例

### 基础插件实现

```csharp
namespace {Namespace};

/// <summary>
/// {PluginSummary}
/// </summary>
public class {PluginName} : PluginBase
{
    /// <inheritdoc />
    public override string Id => "{PluginId}";

    /// <inheritdoc />
    public override string Name => "{PluginName}";

    /// <inheritdoc />
    public override Version Version => new({Version});

    /// <inheritdoc />
    public override string Description => "{PluginDescription}";

    /// <inheritdoc />
    public override IReadOnlyList<string> Dependencies => [
        {Dependencies}
    ];

    /// <inheritdoc />
    protected override Task OnConfigureServicesAsync(
        IPluginServiceConfigurationContext context,
        CancellationToken cancellationToken)
    {
        var config = context.AddPluginConventions<{PluginName}, {ConfigName}>(this);
        {AdditionalRegistration}
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        var asgardContext = GetAsgardContext();
        var logger = CreateLogger();
        logger.LogInformation("插件 {PluginName} 初始化完成", Name);
        {InitializationLogic}
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        var logger = CreateLogger();
        logger.LogInformation("插件 {PluginName} 启动完成", Name);
        {StartLogic}
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnStopAsync(CancellationToken cancellationToken)
    {
        var logger = CreateLogger();
        logger.LogInformation("插件 {PluginName} 停止完成", Name);
        {StopLogic}
        return Task.CompletedTask;
    }
}
```

### 最简入口（Program.cs）

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<{PluginName}>();
```

### 配置文件（plugin.yaml）

```yaml
{PluginName}:
  enabled: {Enabled}
  {ConfigurationSection}:
    {ConfigurationKeys}
```

### 带自动加载作业（plugin.yaml）

```yaml
jobs:
  - name: "{JobName}"
    group: "{GroupName}"
    jobType: "{JobFullTypeName}, {AssemblyName}"
    description: "{JobDescription}"
    triggers:
      - type: cron
        cron: "{CronExpression}"
        startNow: {StartNow}
```

## 推荐做法

- 优先选择 `PluginBase` 继承，不要从零手写 `IPlugin`
- 总是使用 `context.AddPluginConventions<TPlugin, TConfig>` 来自动注册仓储、服务、加载配置
- 依赖通过 `Dependencies` 属性声明，框架会保证加载顺序
- 配置放 `plugin.yaml`，不要混进宿主 `app.yaml`
- 作业定义在 `plugin.yaml`，启动时框架自动注册
- 在正确阶段做正确的事情，`ConfigureServicesAsync` 不要访问 `ServiceProvider`

## 不要这样做

❌ 不要在 `ConfigureServicesAsync` 阶段读取 `ServiceProvider`，此时主机还没构建

❌ 不要为简单场景过早设计复杂的外部插件，先做内建插件验证再拆分

❌ 不要忽略 `AddPluginConventions` 约定后再手写重复扫描注册逻辑

❌ 不要忘记实现 `Id`、`Name`、`Version` 三个必填抽象属性

❌ 不要在错误阶段调用 `GetService`，`PluginBase` 会检查阶段并抛出明确错误

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `IPlugin.cs` - 插件接口定义
- `PluginBase.cs` - 插件基类默认实现

代码范本请参考 `templates/` 目录：
- `BasicPluginImplementation.cs.template` - 基础插件实现范本
- `plugin.yaml.template` - 插件配置范本
- `PluginWithJobs.yaml.template` - 带自动加载作业的配置范本
- `Program-Minimal.cs.template` - 最简入口范本
