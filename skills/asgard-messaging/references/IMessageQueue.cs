namespace Asgard.Abstractions.Messaging;

/// <summary>
/// 消息队列核心接口，提供统一的消息发布和订阅功能。
/// </summary>
/// <remarks>
/// 此接口定义了消息队列的核心操作，当前默认由 RabbitMQ 提供底层实现。
/// 通过统一的抽象，应用层可以在不感知底层通道细节的情况下完成消息发布与消费。
/// </remarks>
/// <example>
/// <para>发布消息示例：</para>
/// <code>
/// public class OrderService
/// {
///     private readonly IMessageQueue _mq;
///     public async Task CreateOrderAsync(Order order)
///     {
///         await _mq.PublishAsync("orders", order, new PublishOptions
///         {
///             Key = order.Id.ToString(),
///             Headers = new Dictionary<string, string>
///             {
///                 ["EventType"] = "OrderCreated"
///             }
///         });
///     }
/// }
/// </code>
/// <para>订阅消息示例：</para>
/// <code>
/// public class OrderConsumer
/// {
///     private readonly IMessageQueue _mq;
///     public async Task StartAsync()
///     {
///         await _mq.SubscribeAsync<Order>("orders", async (message, context) =>
///         {
///             await ProcessOrderAsync(message.Value);
///             await context.AcknowledgeAsync();
///         });
///     }
/// }
/// </code>
/// </example>
public interface IMessageQueue : IAsyncDisposable
{
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
    /// 发布时会根据主题名称与可选消息键决定路由目标。
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
    /// 消息处理完成后，必须调用 <see cref="MessageContext.AcknowledgeAsync"/> 确认消息。
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
    /// 拉取模式适用于需要手动控制消费速度的场景。
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
    /// 批量发布可以提高吞吐量，减少网络往返。
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
