---
name: asgard-configuration
description: "编写或排查 Asgard app.yaml、plugin.yaml、ConfigPath、强类型配置、占位符与加载优先级。host.* 功能细节用 asgard-host-features。"
---

# Asgard Configuration

## 作用

Asgard 使用强类型配置系统，从 YAML 文件、环境变量、命令行参数多个来源合并配置。本 skill 定义了配置类编写、路径绑定、校验、插件配置等约定。

结构与规则边界：

- `plugin.yaml` 默认位于插件主体项目根目录
- `app.yaml` 默认由 starter / host 启动项目加载；单项目快速验证时也可位于同项目根目录
- 配置类默认位于 `Config/PluginConfigs` 或 `Config/{ThirdPartyName}`
- 项目结构见 `$asgard-plugin-structure`
- 编码硬规则见 `$asgard-dotnet-11-csharp-15`

## 什么时候使用

- **需要定义新的系统配置类时** - 创建强类型配置类并绑定到 YAML 路径
- **需要添加宿主配置项时** - 在项目根目录 `app.yaml` 中按照约定添加配置
- **需要为插件添加独立配置时** - 在插件目录创建 `plugin.yaml`
- **需要理解配置覆盖优先级时** - 确认多来源配置的覆盖顺序

## 配置加载优先级

Yggdrasil 8.x 在同一个键有多个值时，按下列顺序选取。表首优先。

| 顺序 | 来源 |
| --- | --- |
| 1 | `CreateBuilder(path)` 显式指定的主 YAML，例如 `app.yaml` |
| 2 | 同目录环境 YAML，例如 `app.Production.yaml` |
| 3 | 进程环境变量，例如 `caching__enabled` |
| 4 | 命令行参数，例如 `--caching:enabled=true` |

例如部署设了 `caching__enabled=true`，主 YAML 写 `caching: { enabled: false }`，最终 Redis 为 `Disabled`。映射按键合并，显式列表整体替换，`[]`、`false` 和 `0` 都有效。需要让部署决定某个值时，可从高优先级文件省略该键，或显式写 `${env:变量名}`。

宿主按主配置和进程配置中的 `host.application.environment` 选环境文件，缺省依次取 `ASPNETCORE_ENVIRONMENT`、`DOTNET_ENVIRONMENT` 和 `Development`。部署可使用 `Integration` 等自定义环境名。`IConfiguration`、强类型对象和配置观察回调使用同一份合并结果。

自定义 `AsgardConfigurationBuilder` 按调用者添加来源的顺序合并，后加入的值生效。`AddPluginConventions` 从该插件的清单绑定配置。需要读取部署变量时在 `plugin.yaml` 中写 `${env:变量名}`，并确保进程实际收到该变量。见 [插件配置注入](../asgard-plugin-development/SKILL.md#插件配置注入硬规则)。

`AfterConfigurationLoad` 执行时配置已经合并，只能读取不可变快照。修改 `host.*` 应在启动前设置配置源；钩子签名和迁移见 [宿主钩子](../asgard-host-project/SKILL.md#构建钩子顺序)。

## 通过 Context 读取已注册配置

8.1 的 `ctx.Config.Get<TConfig>()` 读取当前作用域注册的同一个配置对象，类型约束为 `class, ISystemConfig`。例如已由插件清单装配的 `OrdersConfig` 可写为 `var settings = ctx.Config.Get<OrdersConfig>();`。模块已启用但类型未注册时，调用会抛出明确异常，并记入配置模块的 `LastError`。

运维展示使用 `ctx.Config.Configuration` 的白名单快照。业务读取到的强类型对象可能包含连接串、令牌或密码，只取本次业务需要的字段。`ctx.SystemConfig` 是原有校验与摘要入口。各类容器的默认状态见 [Context 能力表](../asgard-context-usage/references/context-feature-matrix.md)。

自定义配置读取器的契约见 [IAsgardConfig](references/IAsgardConfig.cs)。

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
| 宿主级配置 | `host.*` | `host.kestrel.endpoints.http.url`, `host.openApi.enabled` |
| 基础设施 | `database.*` | 数据库配置 |
| 基础设施 | `caching.*` | 缓存配置 |
| 基础设施 | `messaging.*` | 消息队列配置 |
| 基础设施 | `job.*` | 作业调度配置 |
| 基础设施 | `plugin.*` | 插件宿主配置 |
| 基础设施 | `Asgard.Encryption` | 加密配置 |
| 插件独立配置 | 插件自身路径 | 放在 `plugin.yaml` 中 |

## 代码示例

需要编写该模块代码时，按场景读取 [实现示例](references/implementation-examples.md)，只采用与当前任务和目标版本匹配的示例。

## 推荐做法

- 优先在启动承载项目使用 `app.yaml` 作为宿主主配置文件
- `plugin.yaml` 默认位于插件主体项目根目录；`app.yaml` 由启动承载方加载；两者都不要再套 `config/`
- 配置宿主监听地址时，使用 `host.kestrel.endpoints.*.url`，不要写 `host.port`
- 每个配置类通过 `ConfigPath` 绑定到明确路径，不要散布魔法字符串
- 插件独立配置放 `plugin.yaml`，不要混进宿主 `app.yaml`
- 为可选模块保留默认值与安全降级
- 在 `Validate()` 中提前校验配置，不合法的配置尽早失败
- 扩展现有配置时保留旧键语义，并添加旧 YAML 的兼容性测试

## 不要这样做

❌ 不要把框架配置改写成另一套 JSON 或自定义键名，保持一致性

❌ 不要假设模块启用后所有子配置都完整，始终通过 `Validate()` 校验

❌ 不要把插件配置混进 `app.yaml` 后又在插件内重复定义一份

❌ 不要让非法配置流到运行期再失败，启动时就应该报错

❌ 不要忘记给属性标注 `[ConfigPath]` 特性，否则无法绑定

❌ 不要把宿主端口写成 `host.port`，当前实现不会读取这个键

❌ 不要把 `host.rateLimiting` 根级字段改成 IP 或用户语义；实例总量、IP、用户是三层独立配置

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
