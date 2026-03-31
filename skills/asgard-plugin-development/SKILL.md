---
name: asgard-plugin-development
description: Asgard 内建插件开发 skill。Use when creating, refactoring, explaining, or scaffolding built-in Asgard plugins with PluginBase, PluginWebAppDefaults.RunAsync<TPlugin>(), plugin.yaml, convention-based registration, and plugin-level service composition.
---

# Asgard Built-in Plugin Development

## 作用

这个 skill 只负责 **Asgard 内建插件** 的编写、重构、解释和骨架生成。

- `asgard-plugin-development` 负责回答“插件应该怎么写、怎么组织、先落哪些文件”
- `asgard-plugin-lifecycle` 负责回答“每个生命周期阶段能做什么、不能做什么”

当问题聚焦在阶段顺序、`ServiceProvider` 何时可用、`GetService()` 为什么报错时，优先联动 `$asgard-plugin-lifecycle`。

## 什么时候使用

仅在以下场景触发：

- 创建新的 Asgard 内建插件
- 重构现有内建插件结构
- 生成或整理 `PluginBase` 插件骨架
- 编写或调整 `plugin.yaml`
- 组织插件级服务注册、配置绑定、模块装配
- 解释内建插件推荐目录和代码分层

以下场景不要用本 skill 作为主 skill：

- 外部插件、热插拔目录扫描、`plugins/` 文件系统加载
- 宿主完整构建器与全局中间件编排
- 只讨论生命周期阶段边界

## 默认路线

默认采用最短闭环：

1. 用 `Program.cs` 通过 `PluginWebAppDefaults.RunAsync<TPlugin>()` 启动
2. 插件入口类继承 `PluginBase`
3. 通过 `context.AddPluginConventions<TPlugin, TConfig>()` 注册约定配置
4. 将业务服务装配下沉到模块扩展类
5. 将插件配置和作业统一放进 `plugin.yaml`

除非用户明确要求，否则不要在首版引入外部插件、复杂宿主构建器、扫描加载路径。

## 开发流程

### 1. 先定职责边界

- 明确插件只负责一个业务主题
- 明确插件入口类只承担“声明 + 装配 + 生命周期协调”
- 如果业务模块已经很多，提前规划 `Bootstrap/` 之外的功能目录

### 2. 建立最小项目骨架

先落最小必需文件：

- `Program.cs`
- `Bootstrap/{PluginClassName}.cs`
- `Bootstrap/{FeatureName}ModuleRegistrationExtensions.cs`
- `Bootstrap/Configuration/{PluginConfigClassName}.cs`（需要配置时）
- `plugin.yaml`

目录结构参考 `references/plugin-project-structure.md`。

### 3. 实现 `PluginBase`

优先继承 `PluginBase`，不要从零实现 `IPlugin`。

必须先补齐这些成员：

- `Id`
- `Name`
- `Version`
- `Description`
- `Dependencies`（无依赖时保持空集合）

基础实现模板见：

- `templates/Plugin-Minimal.cs.template`
- `templates/Plugin-WithConfig.cs.template`
- `templates/Plugin-WithJobs.cs.template`

### 4. 注册配置与服务

推荐顺序：

1. 在 `OnConfigureServicesAsync` 里做约定注册
2. 在模块扩展类里注册业务服务、仓储和能力组件
3. 在 `plugin.yaml` 中定义插件配置
4. 需要自动作业时，把作业定义写进 `plugin.yaml`

不要把大量业务注册直接堆进插件入口类。

### 5. 把逻辑放到正确阶段

本 skill 只给出最低限度阶段规则：

- `OnConfigureServicesAsync`：只注册，不解析
- `OnInitializeAsync`：读取配置、拿服务、做轻量初始化
- `OnConfigureMiddlewareAsync`：注册中间件和端点
- `OnStartAsync`：启动后台任务、补启动期准备
- `OnStopAsync`：停止任务、清理资源

如果需要完整阶段说明，读取 `$asgard-plugin-lifecycle`。

### 6. 补全 `plugin.yaml`

`plugin.yaml` 只承载：

- 插件启停配置
- 插件业务配置
- 插件作业配置

不要把宿主级配置、跨插件总线配置、与当前插件无关的全局配置混进去。

### 7. 需要时再扩展

最小闭环跑通之后，再考虑：

- 更细的业务模块拆分
- 更多配置对象
- 更多中间件或 API
- 更多自动作业

不要在第一步把所有扩展点一次铺满。

## 强约束

- 优先走内建插件路线，不默认展开外部插件方案
- 优先继承 `PluginBase`，不要手写整套生命周期
- `OnConfigureServicesAsync` 只注册服务，不构建或解析 `ServiceProvider`
- `GetService<T>()`、`GetOptionalService<T>()`、`CreateLogger()`、`GetAsgardContext()` 只放在 `OnInitializeAsync` 之后
- 优先使用 `context.AddPluginConventions<TPlugin, TConfig>()`
- `Program.cs` 保持一行或极薄入口
- `Bootstrap/` 只放插件入口、配置装配、模块注册相关代码
- `plugin.yaml` 只放插件配置与作业

## 反模式

以下情况通常意味着插件已经开始变乱：

- 在 `OnConfigureServicesAsync` 中 `BuildServiceProvider()`
- 插件入口类同时承担配置绑定、服务注册、业务编排、数据初始化、端点映射
- 把大量业务实现塞进 `Bootstrap/`
- `Program.cs` 写成完整宿主脚本，失去插件项目的极薄入口优势
- `plugin.yaml` 混入宿主配置或其他插件配置
- 尚未跑通最小闭环，就先设计复杂多层抽象

## 按需读取的资源

优先保持 `SKILL.md` 精简，细节按需读取：

- 项目骨架：`references/plugin-project-structure.md`
- 推荐步骤：`references/plugin-development-workflow.md`
- 编写规则与反模式：`references/plugin-authoring-rules.md`
- 接口事实：`references/IPlugin.cs`
- 基类事实：`references/PluginBase.cs`
- 最小模板：`templates/Program-Minimal.cs.template`
- 插件模板：`templates/Plugin-Minimal.cs.template`
- 配置模板：`templates/Plugin-WithConfig.cs.template`
- 作业模板：`templates/Plugin-WithJobs.cs.template`
- 装配模板：`templates/PluginModuleRegistrationExtensions.cs.template`
- 配置文件模板：`templates/plugin.yaml.template`
- 学习示例：`examples/minimal-built-in-plugin.md`
- 进阶示例：`examples/plugin-with-config-and-services.md`
- 重构示例：`examples/plugin-refactor-from-messy-to-clean.md`

## 输出要求

当使用本 skill 生成结果时，默认输出应满足：

- 优先给出最小可运行骨架
- 代码组织先清晰，再追求功能铺满
- 注释解释意图、边界和阶段约束
- 明确哪些代码在插件入口，哪些代码应该下沉到模块或业务目录
- 如果用户的问题实质是生命周期边界，显式提示继续读取 `$asgard-plugin-lifecycle`
