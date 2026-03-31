namespace Asgard.Abstractions.Messaging;

/// <summary>
/// 消息队列核心接口，提供统一的消息发布和订阅功能。
/// </summary>
/// <remarks>
/// 此接口定义了消息队列的核心操作，支持多种消息中间件实现。
/// 通过统一的抽象，应用层可以透明地切换不同的消息队列实现。
/// 支持的消息队列：
/// <list type="bullet">
///   <item>
///     <description>RabbitMQ：支持消息确认、持久化、死信队列等特性。</description>
///   </item>
///   <item>
///     <description>Kafka：支持高吞吐量、分区、消息回溯等特性。</description>
///   </item>
/// </list>
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
    /// 发布消息的行为：
    /// <list type="bullet">
    ///   <item>
    ///     <description>RabbitMQ：消息发送到 Exchange，根据路由键分发到队列。</description>
    ///   </item>
    ///   <item>
    ///     <description>Kafka：消息发送到 Topic 的某个分区。</description>
    ///   </item>
    /// </list>
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
    /// <param name="handler">消息处理委托