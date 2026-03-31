# 内建插件编写规则

## 必须遵守

- 优先继承 `PluginBase`
- 优先使用 `PluginWebAppDefaults.RunAsync<TPlugin>()`
- 优先使用 `context.AddPluginConventions<TPlugin, TConfig>()`
- `OnConfigureServicesAsync` 只做注册，不做解析
- `plugin.yaml` 只放插件配置和插件作业
- 插件入口类只负责声明、装配和生命周期协调

## 生命周期最低限度规则

- 在 `OnConfigureServicesAsync` 中不要构建 `ServiceProvider`
- 在 `OnInitializeAsync` 之前不要调用 `GetService<T>()`
- 中间件和端点放到 `OnConfigureMiddlewareAsync`
- 需要后台任务或启动期动作时，放到 `OnStartAsync`

更完整的阶段说明请配合 `asgard-plugin-lifecycle`。

## 入口类约束

插件入口类应尽量只包含：

- 元数据声明
- 约定注册
- 生命周期钩子
- 少量协调代码

以下内容优先下沉：

- 大量服务注册
- 复杂业务流程
- 业务查询与命令处理
- 端点实现细节
- 数据访问实现

## 常见反模式

### 1. 服务注册阶段偷跑解析

症状：

- `context.Services.BuildServiceProvider()`
- 在注册阶段读取环境、配置或仓储实例

问题：

- 打破宿主构建顺序
- 容易产生重复单例、作用域错误和隐藏依赖

### 2. 插件入口类过胖

症状：

- 一个入口类同时做配置绑定、服务注册、业务初始化、种子数据、端点映射

问题：

- 生命周期边界模糊
- 后续扩展只能继续堆代码

### 3. `Bootstrap/` 变成业务目录

症状：

- 控制器、应用服务、仓储、业务规则都直接堆在启动目录附近

问题：

- 新成员无法快速理解边界
- 修改时容易连带破坏插件装配逻辑

### 4. `plugin.yaml` 失去边界

症状：

- 塞入宿主配置、跨插件配置、与本插件无关的基础设施配置

问题：

- 配置来源不清晰
- 插件无法保持独立演进

## 重构优先级

当插件已经开始混乱时，优先按以下顺序整理：

1. 先恢复 `Program.cs` 极薄入口
2. 再瘦身插件入口类
3. 再补模块扩展类
4. 再拆业务目录
5. 最后再处理更细的优化
