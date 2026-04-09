---
name: asgard-plugin-development
description: Asgard 内建插件开发 skill。Use when implementing built-in plugin entrypoints, PluginBase lifecycle hooks, PluginWebAppDefaults.RunAsync for plugins, AddPluginConventions, plugin bootstrap logic, or plugin.yaml-driven startup behavior in Asgard.
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

- `$asgard-dotnet-10-csharp-14`

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
- 视需要补充认证授权中间件

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
- 将大量服务装配下沉到扩展类或模块装配类
- 把 `plugin.yaml` 作为插件项目中的插件级 YAML 根文件
- 正式开发优先采用“插件项目 + starter 项目分离”
- 所有实现代码都继续遵守 `$asgard-dotnet-10-csharp-14`

## 不要这样做

- ❌ 不要把 starter 项目的 `Program.cs` 默认说成“插件项目入口文件”
- ❌ 不要默认把 `PluginWebAppDefaults.RunAsync<TPlugin>()` 放进插件主体项目
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
