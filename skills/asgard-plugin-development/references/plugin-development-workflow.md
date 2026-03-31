# 内建插件开发流程

## 推荐顺序

1. 明确插件边界
2. 落最小入口
3. 写 `PluginBase` 实现
4. 补配置对象与 `plugin.yaml`
5. 下沉模块注册
6. 再补中间件、作业、初始化逻辑

## 详细步骤

### 1. 明确插件边界

- 明确插件只解决一个业务主题
- 明确插件对外依赖哪些其他插件
- 明确首版需要不要配置、作业、端点

### 2. 落最小入口

先创建：

- `Program.cs`
- `Bootstrap/{PluginClassName}.cs`
- `plugin.yaml`

这一阶段只追求“项目结构成立”，不追求功能完整。

### 3. 写 `PluginBase` 实现

先实现这些成员：

- `Id`
- `Name`
- `Version`
- `Description`
- `Dependencies`

然后只保留最小生命周期钩子：

- `OnConfigureServicesAsync`
- `OnInitializeAsync`
- `OnStartAsync`
- `OnStopAsync`

### 4. 补配置对象与 `plugin.yaml`

当插件需要显式配置时：

- 在 `Bootstrap/Configuration/` 下新增配置类型
- 在 `OnConfigureServicesAsync` 里调用 `context.AddPluginConventions<TPlugin, TConfig>()`
- 在 `plugin.yaml` 中补对应配置段

### 5. 下沉模块注册

一旦出现多于 2 到 3 个服务注册动作，就应新增模块扩展类：

- `Add{FeatureName}Module()`
- `Add{FeatureName}Infrastructure()`

目标是让插件入口类只表达“装配意图”，而不是列出全部实现细节。

### 6. 再补扩展能力

按需增加：

- 中间件与端点
- 自动作业
- 启动期准备逻辑
- 数据预热或轻量种子初始化

## 决策规则

- 如果只是验证插件通路，优先 `Plugin-Minimal.cs.template`
- 如果需要配置绑定，优先 `Plugin-WithConfig.cs.template`
- 如果需要自动作业，优先 `Plugin-WithJobs.cs.template`
- 如果只是服务注册变多，优先新增模块扩展类，不要继续扩写插件入口
