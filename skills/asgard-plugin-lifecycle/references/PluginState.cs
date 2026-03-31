namespace Asgard.Abstractions.Plugin;

/// <summary>
/// 插件状态枚举，定义插件在生命周期中的各种状态。
/// </summary>
/// <remarks>
/// 插件状态转换流程：
/// <list type="bullet">
///   <item><description>正常流程：Unloaded → Loading → Loaded → Initializing → Initialized → Starting → Running</description></item>
///   <item><description>停止流程：Running → Stopping → Stopped</description></item>
///   <item><description>卸载流程：Stopped → Unloading → Unloaded</description></item>
///   <item><description>错误流程：任意状态 → Error</description></item>
/// </list>
/// </remarks>
public enum PluginState
{
    /// <summary>
    /// 未加载状态。
    /// </summary>
    /// <remarks>
    /// 插件尚未被加载到内存中，这是插件的初始状态。
    /// </remarks>
    Unloaded,

    /// <summary>
    /// 加载中状态。
    /// </summary>
    /// <remarks>
    /// 插件正在被加载到内存中，包括程序集加载和类型解析。
    /// </remarks>
    Loading,

    /// <summary>
    /// 已加载未初始化状态。
    /// </summary>
    /// <remarks>
    /// 插件程序集已加载到内存，但尚未执行初始化逻辑。
    /// </remarks>
    Loaded,

    /// <summary>
    /// 初始化中状态。
    /// </summary>
    /// <remarks>
    /// 插件正在执行初始化逻辑，包括资源分配和服务注册。
    /// </remarks>
    Initializing,

    /// <summary>
    /// 已初始化未启动状态。
    /// </summary>
    /// <remarks>
    /// 插件初始化完成，可以随时启动。
    /// </remarks>
    Initialized,

    /// <summary>
    /// 启动中状态。
    /// </summary>
    /// <remarks>
    /// 插件正在启动，准备开始处理业务逻辑。
    /// </remarks>
    Starting,

    /// <summary>
    /// 运行中状态。
    /// </summary>
    /// <remarks>
    /// 插件已完全启动并正在运行，可以正常提供服务。
    /// </remarks>
    Running,

    /// <summary>
    /// 停止中状态。
    /// </summary>
    /// <remarks>
    /// 插件正在停止，正在清理正在处理的请求。
    /// </remarks>
    Stopping,

    /// <summary>
    /// 已停止状态。
    /// </summary>
    /// <remarks>
    /// 插件已停止运行，不再处理新的请求，但仍在内存中。
    /// </remarks>
    Stopped,

    /// <summary>
    /// 卸载中状态。
    /// </summary>
    /// <remarks>
    /// 插件正在从内存中卸载，包括资源释放和程序集卸载。
    /// </remarks>
    Unloading,

    /// <summary>
    /// 错误状态。
    /// </summary>
    /// <remarks>
    /// 插件在加载、初始化、启动或运行过程中发生错误。
    /// 处于此状态的插件可能需要重新加载或修复。
    /// </remarks>
    Error
}
