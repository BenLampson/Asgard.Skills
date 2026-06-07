---
name: asgard-configuration
description: Asgard 配置系统 skill。Use when defining or explaining app.yaml, plugin.yaml, ConfigPath mappings, YAML loading, host and module configuration, configuration precedence, or strongly typed configuration objects in the Asgard framework.
---

# Asgard Configuration

## 作用

Asgard 使用强类型配置系统，从 YAML 文件、环境变量、命令行参数多个来源合并配置。本 skill 定义了配置类编写、路径绑定、校验、插件配置等约定。

结构与规则边界：

- `plugin.yaml` 默认位于插件主体项目根目录
- `app.yaml` 默认由 starter / host 启动项目加载；单项目快速验证时也可位于同项目根目录
- 配置类默认位于 `Config/PluginConfigs` 或 `Config/{ThirdPartyName}`
- 项目结构见 `$asgard-plugin-structure`
- 编码硬规则见 `$asgard-dotnet-10-csharp-14`

## 什么时候使用

- **需要定义新的系统配置类时** - 创建强类型配置类并绑定到 YAML 路径
- **需要添加宿主配置项时** - 在项目根目录 `app.yaml` 中按照约定添加配置
- **需要为插件添加独立配置时** - 在插件目录创建 `plugin.yaml`
- **需要理解配置覆盖优先级时** - 确认多来源配置的覆盖顺序

## 配置加载优先级

优先级从低到高（后加载覆盖先加载）：

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 | YAML 配置文件 | 基础配置，从文件加载 |
| 2 | 环境变量 | 可以覆盖 YAML 配置 |
| 3 | 命令行参数 | 最高优先级，临时覆盖 |

**记住**：后加入的数据覆盖先加入的数据。修复配置问题时先确认覆盖链路，不要只看单个文件。

## 占位符解析

Asgard 支持在 YAML 标量字符串中使用占位符。`app.yaml` 走 `AsgardConfigurationBuilder` 合并配置图时解析，`plugin.yaml` 等直接通过 `YamlConfigLoader.Load/LoadFromFile` 读取的配置也会在绑定前解析。

| 语法 | 作用 | 示例 |
|------|------|------|
| `${配置路径}` | 引用合并后配置图中的另一个值 | `${app.baseUrl}/health` |
| `${env:环境变量名}` | 读取进程环境变量 | `${env:MYSQL_CONNECTION_STRING}` |

示例：

```yaml
database:
  connectionString: "${env:MYSQL_CONNECTION_STRING}"
```

环境变量占位符缺少变量名或对应环境变量未设置时，应在启动/加载阶段直接抛错，不要静默替换为空值。配置路径占位符未命中时保留原始占位符文本。

## 配置类编写约定

| 约定 | 要求 |
|------|------|
| **接口实现** | 必须实现 `ISystemConfig` 接口 |
| **路径绑定** | 每个属性必须标注 `[ConfigPath]` 特性指定 YAML 路径 |
| **默认值** | 通过 `[ConfigPath]` 的 `DefaultValue` 设置默认值 |
| **嵌套配置** | 复杂配置拆分为嵌套类，自动递归绑定 |
| **校验逻辑** | 在 `Validate()` 方法中校验"启用时必须具备的字段" |
| **提前失败** | 非法配置在启动时抛出异常，不要拖到运行期 |

## 标准命名约定

| 配置范围 | 路径前缀 | 示例 |
|----------|----------|------|
| 宿主级配置 | `host.*` | `host.kestrel.endpoints.http.url`, `host.swagger.enabled` |
| 基础设施 | `database.*` | 数据库配置 |
| 基础设施 | `caching.*` | 缓存配置 |
| 基础设施 | `messaging.*` | 消息队列配置 |
| 基础设施 | `job.*` | 作业调度配置 |
| 基础设施 | `plugin.*` | 插件宿主配置 |
| 基础设施 | `Asgard.Encryption` | 加密配置 |
| 插件独立配置 | 插件自身路径 | 放在 `plugin.yaml` 中 |

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

## 推荐做法

- 优先在启动承载项目使用 `app.yaml` 作为宿主主配置文件
- `plugin.yaml` 默认位于插件主体项目根目录；`app.yaml` 由启动承载方加载；两者都不要再套 `config/`
- 配置宿主监听地址时，使用 `host.kestrel.endpoints.*.url`，不要写 `host.port`
- 每个配置类通过 `ConfigPath` 绑定到明确路径，不要散布魔法字符串
- 插件独立配置放 `plugin.yaml`，不要混进宿主 `app.yaml`
- 为可选模块保留默认值与安全降级
- 在 `Validate()` 中提前校验配置，不合法的配置尽早失败

## 不要这样做

❌ 不要把框架配置改写成另一套 JSON 或自定义键名，保持一致性

❌ 不要假设模块启用后所有子配置都完整，始终通过 `Validate()` 校验

❌ 不要把插件配置混进 `app.yaml` 后又在插件内重复定义一份

❌ 不要让非法配置流到运行期再失败，启动时就应该报错

❌ 不要忘记给属性标注 `[ConfigPath]` 特性，否则无法绑定

❌ 不要把宿主端口写成 `host.port`，当前实现不会读取这个键

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `ISystemConfig.cs` - 配置接口定义
- `ConfigPathAttribute.cs` - 配置路径特性
- `AsgardConfigurationBuilder.cs` - 配置构建器
- `AsgardConfigurationRoot.cs` - 合并配置根与占位符解析
- `YamlConfigLoader.cs` - YAML 配置加载器
- `PluginConventions.cs` - 插件配置加载约定

结构规范请参考 `$asgard-plugin-structure`。

代码范本请参考 `templates/` 目录，可直接替换占位符使用。
