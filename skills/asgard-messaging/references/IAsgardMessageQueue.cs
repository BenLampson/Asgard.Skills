namespace Asgard.Abstractions.Messaging;

/// <summary>业务作用域的消息队列入口，提供发布、消费、健康状态与追踪能力。</summary>
public interface IAsgardMessageQueue : IAsgardFeature
{
    /// <summary>消息追踪能力；未配置时可读取 Disabled 状态。</summary>
    IMessageTracer Tracer => MessageTracerFeature.Disabled(this);

    /// <summary>
    /// 发布消息到指定主题。
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题名称</param>
    /// <param name="message">消息内容</param>
    /// <param name="options">发布选项（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 发布时默认根据主题名称决定路由目标；需要覆盖底层路由时使用 <see cref="PublishOptions.RoutingKey"/>。
    /// 即时发布默认在 broker 确认且已路由后完成，未路由会抛出异常。broker 延迟发布完成表示已接受调度。
    /// </remarks>
    /// <exception cref="ArgumentNullException">topic 或 message 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当消息队列未连接时抛出</exception>
    Task PublishAsync<T>(string topic, T message, PublishOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布消息包装类到指定主题。
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题名称</param>
    /// <param name="message">消息包装类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 使用消息包装类可以携带更多元数据，如消息 ID、头部信息、时间戳等。
    /// </remarks>
    [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
    Task PublishAsync<T>(string topic, Message<T> message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅指定主题的消息。
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题名称</param>
    /// <param name="handler">消息处理委托</param>
    /// <param name="options">订阅选项（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>订阅标识，用于取消订阅</returns>
    /// <remarks>
    /// 订阅后，底层通道会把命中的消息分发给处理委托。
    /// 手动确认模式下，处理成功后调用 <see cref="MessageContext.AcknowledgeAsync"/>。自动确认由 broker 在投递时完成。
    /// </remarks>
    Task<string> SubscribeAsync<T>(string topic, Func<Message<T>, MessageContext, Task> handler, SubscribeOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消订阅。
    /// </summary>
    /// <param name="subscriptionId">订阅标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 取消订阅后，相关的消费者资源会被释放。
    /// </remarks>
    Task UnsubscribeAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 拉取指定主题的消息（非阻塞）。
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息和上下文，如果没有可用消息则返回 null</returns>
    /// <remarks>
    /// 取消令牌控制本次拉取。取得消息后仍可通过返回的上下文确认、拒绝或重新入队。
    /// </remarks>
    Task<(Message<T>? Message, MessageContext? Context)?> PullAsync<T>(string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量发布消息。
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题名称</param>
    /// <param name="messages">消息集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 默认 RabbitMQ 实现按输入顺序逐条发布。若某条发布失败，此前已发布的消息仍然有效。
    /// </remarks>
    Task PublishBatchAsync<T>(string topic, IEnumerable<Message<T>> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取消息队列的健康状态。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否健康</returns>
    /// <remarks>
    /// 检查消息队列连接是否正常，可用于健康检查端点。
    /// </remarks>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
