---
name: asgard-plugin-development
description: Asgard 内建插件开发 skill。Use when implementing built-in plugin entrypoints, PluginBase lifecycle hooks, PluginWebAppDefaults.RunAsync for plugins, AddPluginConventions, plugin bootstrap logic, or plugin.yaml-driven startup behavior in Asgard.
---

# Asgard Plugin Development

## 作用

这个 skill 只负责 **Asgard 内建插件入口与加载逻辑**。

它关注：

- `PluginBase`
- `PluginWebAppDefaults.RunAsync<TPlugin>()`
- `AddPluginConventions`
- 插件入口类职责
- `plugin.yaml` 的插件级使用方式
- 生命周期与启动装配的衔接

它不负责定义完整目录结构，也不负责定义编码硬规则。

结构问题统一见：

- `$asgard-plugin-structure`

强制编码规则统一见：

- `$asgard-dotnet-10-csharp-14`

生命周期边界统一见：

- `$asgard-plugin-lifecycle`

## 什么时候使用

- 创建新的内建插件入口类
- 使用 `PluginBase` 编写插件
- 编写 `Program.cs` 插件入口
- 在插件中使用 `AddPluginConventions`
- 调整 `plugin.yaml` 的插件级配置
- 解释插件加载、初始化、启动、停止逻辑

以下场景不要用本 skill 作为结构权威：

- 目录怎么搭
- `DTO / VO / Entity` 应该放哪里
- `Config / Controllers / Services / Domains` 的目录边界

## 最小开发路径

1. 先按 `$asgard-plugin-structure` 搭项目骨架
2. 在 `Program.cs` 使用 `PluginWebAppDefaults.RunAsync<TPlugin>()`
   如果插件自己注册了认证/授权服务，则同时在回调中补上 `UseAuthentication()` 与 `UseAuthorization()`
3. 插件入口类继承 `PluginBase`
4. 在 `OnConfigureServicesAsync` 中使用 `context.AddPluginConventions<TPlugin, TConfig>()`
5. 把插件级配置放在项目根目录的 `plugin.yaml`
6. 生命周期边界细节交给 `$asgard-plugin-lifecycle`

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
- 使用 `Program.cs` 的最短入口
- 优先使用 `context.AddPluginConventions<TPlugin, TConfig>()`
- 将大量服务装配下沉到扩展类或模块装配类
- 把 `plugin.yaml` 作为插件级 YAML 根文件使用
- 所有实现代码都继续遵守 `$asgard-dotnet-10-csharp-14`

## 不要这样做

- ❌ 不要在本 skill 中自行决定目录结构，目录问题交给 `$asgard-plugin-structure`
- ❌ 不要在 `OnConfigureServicesAsync` 里构建或解析 `ServiceProvider`
- ❌ 不要把生命周期规则写成另一套，阶段边界交给 `$asgard-plugin-lifecycle`
- ❌ 不要发明与 `$asgard-dotnet-10-csharp-14` 冲突的编码写法

## 参考资料

- `references/IPlugin.cs`
- `references/PluginBase.cs`
- `templates/Program-Minimal.cs.template`
- `templates/Plugin-Minimal.cs.template`
- `templates/Plugin-WithConfig.cs.template`
- `templates/Plugin-WithJobs.cs.template`
- `templates/PluginModuleRegistrationExtensions.cs.template`
- `templates/plugin.yaml.template`
