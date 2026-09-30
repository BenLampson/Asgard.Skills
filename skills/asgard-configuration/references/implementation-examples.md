## 代码示例

### 宿主端口配置

当前 Asgard 宿主实现通过 `host.kestrel.endpoints.*.url` 配置监听地址与端口，不能写成 `host.port`。

```yaml
host:
  kestrel:
    endpoints:
      http:
        url: "http://127.0.0.1:4321"
```

如果需要 HTTPS：

```yaml
host:
  kestrel:
    endpoints:
      https:
        url: "https://0.0.0.0:5001"
        certificate:
          path: "certs/dev.pfx"
          password: "your-password"
```

### 宿主分层限流配置

`host.rateLimiting` 的兼容契约不能随意改名或重新解释：根级扁平字段是当前宿主实例共享的总量桶，`ip`、`user` 是可选子配置。旧 YAML 不包含两个子节点时，行为保持为单实例总量限流。

```yaml
host:
  rateLimiting:
    enabled: true
    policy: FixedWindow
    permitLimit: 600
    windowSeconds: 60
    queueLimit: 0

    ip:
      enabled: true
      policy: FixedWindow
      permitLimit: 100
      windowSeconds: 60
      queueLimit: 0

    user:
      enabled: true
      policy: FixedWindow
      permitLimit: 60
      windowSeconds: 60
      queueLimit: 0
```

强类型绑定约定：

- `RateLimitingOptions` 的根级属性使用完整路径，例如 `[ConfigPath("host.rateLimiting.permitLimit")]`。
- `Ip`、`User` 入口分别绑定 `[ConfigPath("host.rateLimiting.ip")]`、`[ConfigPath("host.rateLimiting.user")]`。
- `RateLimitingPartitionOptions` 是嵌套对象，其属性使用相对路径，例如 `[ConfigPath("enabled")]`、`[ConfigPath("policy")]`。
- 子层缺失应保持为 `null`，子层存在但 `enabled: false` 时不校验其算法参数；`host.rateLimiting.enabled` 仍是三层总开关。
- 修改该配置契约时必须覆盖“旧版扁平 YAML 仍可加载”和“嵌套 IP/用户配置能递归绑定”两类测试。

运行时分层语义和中间件顺序由 `$asgard-host-features` 定义；配置 skill 不应把根级字段描述为 IP 限流，也不要发明 `partitionBy` 等当前不存在的 YAML 键。

### 强类型配置类

```csharp
namespace {Namespace}.Config.PluginConfigs;

/// <summary>
/// {ConfigSummary}
/// </summary>
public class {ConfigName} : ISystemConfig
{
    /// <summary>
    /// 是否启用此模块
    /// </summary>
    [ConfigPath("{ModuleName}.enabled", DefaultValue = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// {PropertySummary}
    /// </summary>
    [ConfigPath("{ModuleName}.{PropertyPath}", DefaultValue = {DefaultValue})]
    public {PropertyType} {PropertyName} { get; set; } = {DefaultInitializer};

    /// <summary>
    /// 嵌套配置选项
    /// </summary>
    [ConfigPath("{ModuleName}.{NestedSection}")]
    public {NestedConfigType} {NestedConfigName} { get; set; } = new();

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        // 模块启用时需要验证必填配置
        {ValidationCode}
    }
}
```

### 插件配置加载

项目根目录中的 `plugin.yaml`：

```yaml
{PluginName}:
  enabled: {Enabled}
  {PropertyName}: {PropertyValue}
```

插件启动时加载配置：

```csharp
protected override Task OnConfigureServicesAsync(
    IPluginServiceConfigurationContext context,
    CancellationToken cancellationToken)
{
    var config = context.AddPluginConventions<{PluginName}, {ConfigName}>();
    {AdditionalRegistration}
    return Task.CompletedTask;
}
```
