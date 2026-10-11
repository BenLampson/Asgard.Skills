// 源码快照：Asgard 6.0.1 基础设施生命周期修复（2026-10-08 核对；发布状态另行确认）；使用时确认目标版本包含修复。
namespace Asgard.Abstractions.Messaging;

/// <summary>
/// 消息订阅选项。
/// </summary>
/// <remarks>
/// 用于配置消息订阅时的行为，如消费者组、自动确认、预取数量等。
/// </remarks>
/// <example>
/// 使用示例：
/// <code>
/// var options = new SubscribeOptions
/// {
///     GroupId = "order-processor",
///     AutoAck = false,
///     PrefetchCount = 10,
///     MaxRetryCount = 3
/// };
/// await _mq.SubscribeAsync&lt;Order&gt;("orders", HandleOrder, options);
/// </code>
/// </example>
public class SubscribeOptions
{
    /// 获取或设置是否启用自动确认。
    /// </summary>
    /// <remarks>
    /// 自动确认行为：
    /// <list type="bullet">
    ///   <item><description>true：broker 投递即确认，上下文确认/拒绝为无操作，处理失败不重试，可能导致消息丢失。</description></item>
    ///   <item><description>false：需要手动调用 AcknowledgeAsync 确认。</description></item>
    /// </list>
    /// 生产环境建议设置为 false，确保消息可靠性。
    /// </remarks>
    /// <value>是否启用自动确认，默认为 false。</value>
    public bool AutoAck { get; set; } = false;

    /// <summary>
    /// 获取或设置预取数量。
    /// </summary>
    /// <remarks>
    /// 预取数量控制消费者同时处理的消息数量。
    /// 在当前默认实现中，会映射为 RabbitMQ 的 QoS 预取值。
    /// 较大的值提高吞吐量，但增加内存占用。
    /// </remarks>
    /// <value>预取数量，默认为 10。</value>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// 获取或设置最大重试次数。
    /// </summary>
    /// <remarks>
    /// 当消息处理失败时，会进行重试。
    /// 默认 RabbitMQ 实现将计数保存在消息头并精确转发到源队列，超过预算后转发到死信队列或丢弃。
    /// 转发必须完成 publisher confirm 和 mandatory 路由检查后才确认原消息；结果不确定时保留原消息未确认。
    /// 此时应检查目标队列后重建通道或连接（仅取消订阅不会恢复未确认投递）；恢复可能重复投递，处理器仍需具备幂等性。
    /// 调用方显式 RejectAsync(true) 仍直接重入队，不属于此自动重试预算。
    /// </remarks>
    /// <value>最大重试次数，默认为 3。</value>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 获取或设置是否从最早的消息开始消费。
    /// </summary>
    /// <remarks>
    /// 该选项为消息消费流程预留入口。
    /// 当前默认 RabbitMQ 实现不会使用该值决定历史消息起点。
    /// </remarks>
    /// <value>是否从最早的消息开始消费，默认为 false。</value>
    public bool FromBeginning { get; set; } = false;

    /// <summary>
    /// 获取或设置死信队列名称。
    /// </summary>
    /// <remarks>
    /// 当消息处理失败且超过重试次数后，会被发送到死信队列。
    /// 如果为 null，使用默认死信队列命名规则；自定义名称使用 RabbitMQ QueuePrefix。
    /// AutoDeclare=false 且 AutoAck=false 时该队列必须由外部预先创建，订阅只做被动检查。
    /// 框架失败和上下文 RejectAsync(false) 直接转发，不依赖源队列遗留的 DLX 参数。
    /// 由 broker 的 TTL 或长度策略触发的死信仍使用外部 DLX 拓扑，需要单独配置。
    /// </remarks>
    /// <value>死信队列名称。</value>
    public string? DeadLetterQueue { get; set; }

    /// <summary>
    /// 获取或设置是否启用死信队列。
    /// </summary>
    /// <remarks>
    /// 启用后，处理失败的消息会被发送到死信队列，而不是直接丢弃。
    /// </remarks>
    /// <value>是否启用死信队列，默认为 true。</value>
    public bool EnableDeadLetter { get; set; } = true;

    /// <summary>
    /// 获取或设置消费者标签。
    /// </summary>
    /// <remarks>
    /// 用于标识消费者，便于监控和管理。
    /// 如果为 null，系统会自动生成唯一标签。
    /// </remarks>
    /// <value>消费者标签。</value>
    public string? ConsumerTag { get; set; }

    /// <summary>
    /// 获取或设置是否独占消费。
    /// </summary>
    /// <remarks>
    /// 独占消费：
    /// <list type="bullet">
    ///   <item><description>true：只有一个消费者可以消费该队列。</description></item>
    ///   <item><description>false：多个消费者可以同时消费。</description></item>
    /// </list>
    /// 仅 RabbitMQ 支持。
    /// </remarks>
    /// <value>是否独占消费，默认为 false。</value>
    public bool Exclusive { get; set; } = false;

    /// <summary>
    /// 获取或设置消息处理超时时间。
    /// </summary>
    /// <remarks>
    /// 超时后，消息会被视为处理失败，触发重试或死信逻辑。
    /// </remarks>
    /// <value>消息处理超时时间，默认为 30 秒。</value>
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 获取或设置重试间隔（毫秒）。
    /// </summary>
    /// <remarks>
    /// 消息处理失败后的重试间隔。
    /// 可以设置递增间隔，如 [1000, 2000, 5000]。
    /// </remarks>
    /// <value>重试间隔（毫秒）数组。</value>
    public int[]? RetryIntervals { get; set; }
}
