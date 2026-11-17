---
name: asgard-plugin-development
description: "实现 Asgard 内建插件入口、PluginBase、AddPluginConventions 和 plugin.yaml 装配。Program.cs 启动编排用 asgard-host-project。"
---

# Asgard Plugin Development

## 作用

这个 skill 只负责 **Asgard 内建插件实现与加载逻辑**。

它关注：

- `PluginBase`
- `PluginWebAppDefaults.RunAsync<TPlugin>()`
- `AddPluginConventions`
- 插件入口类职责
- `plugin.yaml` 的插件级使用方式
- 生命周期与启动装配的衔接
- 插件主体项目与 starter 启动项目的入口边界

它不负责定义完整目录结构，也不负责定义编码硬规则。

结构问题统一见：

- `$asgard-plugin-structure`

强制编码规则统一见：

- `$asgard-dotnet-11-csharp-15`

生命周期边界统一见：

- `$asgard-plugin-lifecycle`

## 什么时候使用

- 创建新的内建插件入口类
- 使用 `PluginBase` 编写插件
- 编写 starter / 启动项目中的 `Program.cs`
- 在插件中使用 `AddPluginConventions`
- 调整 `plugin.yaml` 的插件级配置
- 解释插件加载、初始化、启动、停止逻辑

以下场景不要用本 skill 作为结构权威：

- 目录怎么搭
- `DTO / VO / Entity` 应该放哪里
- `Config / Controllers / Services / Domains` 的目录边界

## 先区分两个“入口”

- 插件入口类：
  继承 `PluginBase` 的插件类，例如 `OidcPlugin.cs`
- 启动入口：
  starter / 宿主启动项目中的 `Program.cs`

不要把这两个入口混为一谈。

## 最小开发路径

1. 先按 `$asgard-plugin-structure` 判断当前是单项目快速验证，还是双项目分离
2. 编写插件主体项目中的插件入口类，并继承 `PluginBase`
3. 在 starter 项目的 `Program.cs` 中使用 `PluginWebAppDefaults.RunAsync<TPlugin>()`
   如果插件自己注册了认证/授权服务，则同时在回调中补上 `UseAuthentication()` 与 `UseAuthorization()`
4. 在 `OnConfigureServicesAsync` 中使用 `context.AddPluginConventions<TPlugin, TConfig>()`
5. 把插件级配置放在插件项目中的 `plugin.yaml`
6. 由启动承载方决定 `app.yaml` 的加载位置
7. 生命周期边界细节交给 `$asgard-plugin-lifecycle`

## PluginSdk 覆盖边界

默认先假设 `Asgard.PluginSdk` 已经处理插件常规装配，不要重复手写。

`PluginWebAppDefaults.RunAsync<TPlugin>()` 已覆盖：

- 创建 Yggdrasil 宿主
- 注册内建插件
- 应用推荐插件 Web 中间件默认值

`context.AddPluginConventions<TPlugin, TConfig>()` 已覆盖：

- 扫描插件程序集中的 `[Repository]` 仓储
- 扫描插件程序集中的 `[Service]` 服务
- 从宿主选定的插件清单绑定强类型配置，校验后注册具体实例

仍需要显式处理的通常是非约定能力：

- SignalR、gRPC、第三方 SDK client 等框架外服务
- `MapHub<T>`、自定义 endpoint、中间件挂载
- 数据库结构同步或迁移，例如 `CodeFirst.SyncStructure(...)`
- 不符合 `[Repository]` / `[Service]` 约定的特殊生命周期服务

如果需求只是“注册服务、仓储、插件配置”，默认写 `AddPluginConventions`，不要再生成一串 `AddScoped`、`AddRepositories`、`AddServices`。

Asgard 7.1 起，`plugin.enabled` 只控制外部 DLL 列表和目录扫描。代码显式注册的内建插件仍参与装配；启用外部加载时，显式启用的 DLL 缺失会阻止宿主启动。

## 插件配置注入硬规则

`AddPluginConventions<TPlugin, TConfig>()` 注册的是 **具体配置类型 `TConfig`**。业务服务、作业和生命周期回调分别通过构造函数或 `GetService<TConfig>()` 直接获取它。例如把作业构造函数的 `IOptions<CleanupConfig> options` 改为 `CleanupConfig config`，并将 `options.Value.DryRun` 改为 `config.DryRun`。

- 不使用 `IOptions<TConfig>`、`IOptionsSnapshot<TConfig>` 或 `IOptionsMonitor<TConfig>`，也不在宿主中为该配置另做 Options 绑定。旧包可能让 Options 返回一个默认对象；包含本次插件契约的框架在解析时明确报错。宿主及第三方自己的 Options 不受影响。
- 每种配置用自己的类型。配置和自动作业共享同一份清单快照；`ISystemConfig.Validate()` 在注册前执行。配置必须随部署交付时，用 `context.AddPluginConventions<TPlugin, TConfig>(requireManifest: true)`，可选清单缺失时仍应用并校验默认值。使用该参数前确认目标包已包含新契约；已发布的 7.1.0 可以先完成直接注入迁移。
- 已装配插件需要另外读取时用 `plugin.LoadPluginConfig<TConfig>()`。脱离宿主的工具必须传具体文件给 `YamlConfigLoader.LoadPluginConfigFromFile<TConfig>(manifestPath)`。多个插件共用输出目录时各用 `plugin.<插件 ID>.yaml`。

## 插件路由和外部发现

- 插件 Controller 由已装配的插件公开。一个程序集含多个插件且分别装配时，给 Controller 加 `[PluginController(typeof(所属插件))]`（`Asgard.Abstractions.AspNetCore.Controller`），或拆成各自程序集。Controller 单独放在不含插件入口的程序集时，也用该特性声明归属。
- 扫描外部 DLL 时，在入口旁提供 `<入口名>.manifest.json`，例如 `Orders.Plugin.dll` 对应 `Orders.Plugin.manifest.json`，内容为 `{"id":"orders"}`。显式启用项的配置 ID、扫描清单 ID 都必须与 `IPlugin.Id` 一致；禁用项和 `excludePlugins` 优先，改目录名也不能重新启用。
- 路由归属特性、扫描标识清单和配置错误机制适用于包含新插件契约的目标版本，生成项目时核对实际引用的 Asgard 包。

## 插件入口类职责

插件入口类默认只负责：

- 声明 `Id`
- 声明 `Name`
- 声明 `Version`
- 声明 `Description`
- 声明 `Dependencies`
- 执行插件级服务装配
- 串接初始化、启动、停止阶段

不要把完整业务实现塞进插件入口类。

## 启动入口职责

starter 项目的 `Program.cs` 默认负责：

- 选择启动路径
- 调用 `PluginWebAppDefaults.RunAsync<TPlugin>()` 或 `YggdrasilHost.CreateBuilder(...)`
- 决定 `app.yaml` 加载路径
- 解析启动参数
- 视需要补充宿主认证接线或授权中间件

其中：

- 宿主默认 JWT / `host.auth` 接线优先看 `$asgard-host-features`
- Web 登录流、OIDC / PKCE、IDP 对接优先看 `$identity-integration`
- `AsgardAuth` 授权 DSL 和声明式授权优先看 `$asgard-auth-authorization`

`PluginWebAppDefaults.RunAsync<TPlugin>()` 通常应位于 starter，而不是业务插件主体项目。

## 生命周期最低要求

- `OnConfigureServicesAsync`
  只做注册，不解析服务
- `OnInitializeAsync`
  可读取配置、解析服务、创建日志器
- `OnConfigureMiddlewareAsync`
  可注册中间件和端点
- `OnStartAsync`
  可执行启动期动作
- `OnStopAsync`
  可执行停止期清理

更完整的阶段规则请读取 `$asgard-plugin-lifecycle`。

## 推荐做法

- 优先继承 `PluginBase`
- 优先把 `Program.cs` 放在 starter / 启动项目
- 优先使用 `context.AddPluginConventions<TPlugin, TConfig>()`
- 只有 `AddPluginConventions` 未覆盖的能力，才下沉到扩展类或模块装配类
- 把 `plugin.yaml` 作为插件项目中的插件级 YAML 根文件
- 正式开发优先采用“插件项目 + starter 项目分离”
- 所有实现代码都继续遵守 `$asgard-dotnet-11-csharp-15`

## 不要这样做

- ❌ 不要把 starter 项目的 `Program.cs` 默认说成“插件项目入口文件”
- ❌ 不要默认把 `PluginWebAppDefaults.RunAsync<TPlugin>()` 放进插件主体项目
- ❌ 不要在插件里重复手写 `[Repository]` / `[Service]` 能被 `AddPluginConventions` 扫描到的仓储和服务注册
- ❌ 不要因为存在 Hub、脚本、任务或 Agent 相关实体，就默认拆出 `.Web` / `.Agent` 项目；结构边界交给 `$asgard-plugin-structure`
- ❌ 不要把数据库结构同步误认为 `PluginSdk` 已经自动完成；除非框架已有专门约定，否则需要插件显式处理
- ❌ 不要在本 skill 中自行决定目录结构，目录问题交给 `$asgard-plugin-structure`
- ❌ 不要在 `OnConfigureServicesAsync` 里构建或解析 `ServiceProvider`
- ❌ 不要把生命周期规则写成另一套，阶段边界交给 `$asgard-plugin-lifecycle`
- ❌ 不要发明与 `$asgard-dotnet-11-csharp-15` 冲突的编码写法

## 参考资料

- `references/IPlugin.cs`
- `references/PluginBase.cs`
- `templates/Program-Minimal.cs.template`
- `templates/Plugin-Minimal.cs.template`
- `templates/Plugin-WithConfig.cs.template`
- `templates/Plugin-WithJobs.cs.template`
- `templates/PluginModuleRegistrationExtensions.cs.template`
- `templates/plugin.yaml.template`
