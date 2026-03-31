# 最小内建插件示例

这个示例用于快速搭出一个能运行的 Asgard 内建插件骨架。

## 文件结构

```text
DemoPlugin/
├── Program.cs
├── plugin.yaml
└── Bootstrap/
    └── DemoPlugin.cs
```

## Program.cs

```csharp
using Asgard.PluginSdk;

await PluginWebAppDefaults.RunAsync<DemoPlugin>();
```

## 插件入口

```csharp
namespace DemoPlugin.Bootstrap;

/// <summary>
/// 演示插件入口。
/// </summary>
public sealed class DemoPlugin : PluginBase
{
    /// <summary>
    /// 插件唯一标识。
    /// </summary>
    public override string Id => "demo-plugin";

    /// <summary>
    /// 插件显示名称。
    /// </summary>
    public override string Name => "Demo Plugin";

    /// <summary>
    /// 插件版本号。
    /// </summary>
    public override Version Version => new(1, 0, 0);

    /// <summary>
    /// 插件描述。
    /// </summary>
    public override string Description => "用于演示 Asgard 内建插件最小骨架。";
}
```

## plugin.yaml

```yaml
demo-plugin:
  enabled: true
```

## 适用场景

- 验证插件项目是否能跑起来
- 验证 `PluginBase` 继承链是否正确
- 为后续增加配置、服务、作业做起点
