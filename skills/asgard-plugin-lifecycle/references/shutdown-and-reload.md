# 插件关闭与重载

以下说明 Asgard 7.x 的关闭和重载契约。核对实现时查看 Asgard 的 `PluginManager.cs`、`PluginManager.Lifecycle.cs`、`PluginLoaderHelper.cs` 和 `PluginServiceConfigurator.cs`。

## 使用边界

Asgard 7.1 的 Yggdrasil 在构建时固定插件服务和路由，宿主管理器拒绝 `UnloadAsync`、`UnloadWithDependentsAsync` 和 `ReloadAsync`。替换或移除 Web 插件应调整配置并重启宿主。整体停止仍会回收插件；以下单插件操作描述适用于独立的 `PluginManager`。

- 批量停止与清理使用既有依赖拓扑逆序。A 依赖 B 时，先退出 A，再退出 B；不要依靠字典插入顺序或仅计算依赖数量
- `UnloadAsync(pluginId)` 仅卸载指定插件；需要连同依赖方卸载时调用 `UnloadWithDependentsAsync(pluginId)`，不要误以为单插件卸载自动处理依赖图
- 对运行中的插件尝试 Stop，再独立尝试 Dispose 和可收集加载上下文的 Unload。Stop 失败/取消不跳过后两步；Dispose 失败也不跳过 Unload
- 批量清理继续处理其他插件，完成尝试后汇总抛出错误。记录所有内部错误，不把“抛异常”理解为所有清理均未进行
- 管理器释放也包括已发现但尚未初始化/初始化失败的实例。重复释放不会再次执行整批清理
- 已知但已卸载插件的重复 Unload 安全返回；未知插件 ID 仍报错，不把拼错 ID 当成功

包含新插件契约的框架将插件启动移至宿主选项校验之后。仅 Build 的宿主释放插件时调用 Dispose；只有已尝试启动的实例才需 Stop。用框架运行入口保证端口绑定等后续启动失败也会先 Stop 再释放容器，并保留原始启动错误。

## 资源所有权

插件负责自己创建的后台任务、事件订阅和连接。停止后台任务并取消订阅事件，避免外部对象继续引用插件。宿主提供的共享服务只是借用，不由每个插件分别 Dispose。

卸载会移除管理器持有的描述符、插件实例和加载上下文引用，仅保留复制后的纯元数据供 Reload。CLR 的实际程序集回收依赖引用消失与 GC；宿主可能仍通过 MVC/反射类型/单例/事件持有引用，所以不能宣称完整热卸载或立即回收成功。

## Reload

- 外部插件 Reload 从保留元数据重新加载并初始化目标插件，不复用旧实例或描述对象
- 内建插件与宿主共享加载上下文，不支持 Reload
- Reload 只重建目标本身，不重建整个宿主依赖图，也不承诺自动重建 MVC/DI 注册或启动运行
- 重载初始化失败时仍释放新实例；原始失败与清理失败都保留，不能只处理最后一个异常

## 验证

修改这些流程时运行目标源码的 `PluginCleanupTests`、`PluginServiceConfiguratorCleanupTests` 及加载上下文相关测试，覆盖逆拓扑、Stop/Dispose 分别失败、未初始化实例和重复清理。隔离可收集上下文测试通过不等于运行中宿主的所有插件引用都已释放。
