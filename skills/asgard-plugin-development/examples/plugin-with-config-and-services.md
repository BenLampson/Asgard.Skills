# 带配置与服务注册的插件示例

这个示例展示插件入口、配置对象、模块注册扩展类之间如何协作。

## 文件结构

```text
InventoryPlugin/
├── Program.cs
├── plugin.yaml
└── Bootstrap/
    ├── InventoryPlugin.cs
    ├── InventoryModuleRegistrationExtensions.cs
    └── Configuration/
        └── InventoryPluginConfig.cs
```

## 插件入口

```csharp
namespace InventoryPlugin.Bootstrap;

/// <summary>
/// 库存插件入口。
/// </summary>
public sealed class InventoryPlugin : PluginBase
{
    /// <summary>
    /// 插件唯一标识。
    /// </summary>
    public override string Id => "inventory-plugin";

    /// <summary>
    /// 插件显示名称。
    /// </summary>
    public override string Name => "Inventory Plugin";

    /// <summary>
    /// 插件版本号。
    /// </summary>
    public override Version Version => new(1, 0, 0);

    /// <summary>
    /// 插件描述。
    /// </summary>
    public override string Description => "提供库存能力。";

    /// <summary>
    /// 注册插件配置与业务模块。
    /// </summary>
    protected override Task OnConfigureServicesAsync(
        IPluginServiceConfigurationContext context,
        CancellationToken cancellationToken)
    {
        _ = context.AddPluginConventions<InventoryPlugin, InventoryPluginConfig>(this);
        _ = context.Services.AddInventoryModule();
        return Task.CompletedTask;
    }
}
```

## 模块注册扩展类

```csharp
namespace InventoryPlugin.Bootstrap;

/// <summary>
/// 库存模块注册扩展。
/// </summary>
public static class InventoryModuleRegistrationExtensions
{
    /// <summary>
    /// 注册库存模块服务。
    /// </summary>
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        _ = services.AddScoped<IInventoryService, InventoryService>();
        _ = services.AddScoped<IInventoryRepository, InventoryRepository>();
        return services;
    }
}
```

## 配置对象

```csharp
namespace InventoryPlugin.Bootstrap.Configuration;

/// <summary>
/// 库存插件配置。
/// </summary>
public sealed class InventoryPluginConfig
{
    /// <summary>
    /// 是否启用库存预热。
    /// </summary>
    public bool EnableWarmup { get; set; }
}
```

## plugin.yaml

```yaml
inventory-plugin:
  enabled: true
  inventory:
    enableWarmup: true
```

## 关键点

- 插件入口只保留约定注册和装配动作
- 具体服务注册下沉到模块扩展类
- 配置对象放到 `Bootstrap/Configuration/`
- 当模块继续膨胀时，再向业务目录拆分
