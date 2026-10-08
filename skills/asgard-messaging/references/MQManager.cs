// 源码快照：Asgard 6.0.1 基础设施生命周期修复（2026-10-08 核对；发布状态另行确认）；使用时确认目标版本包含修复。
namespace Asgard.Core.Messaging;

/// <summary>
/// 消息队列管理器实现，负责消息队列系统的初始化、连接验证和生命周期管理。
/// </summary>
/// <remarks>
/// <para>
/// 此类独立于 IoC 容器，在系统启动阶段（Phase 3）完成初始化和连接验证。
/// 初始化时会创建 RabbitMQ 连接，并通过健康检查验证可用性。
/// </para>
/// </remarks>
public sealed class MQManager : IMQManager
{
    private readonly MQConfig _config;
    private readonly ILogger<MQManager> _logger;
    private IMessageQueue? _messageQueue;
    private readonly Func<MQConfig, IMessageQueue> _queueFactory;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="MQManager"/> 类的新实例。
    /// </summary>
    /// <param name="config">消息队列配置。</param>
    /// <param name="logger">日志器。</param>
    public MQManager(MQConfig config, ILogger<MQManager> logger)
        : this(config, logger, static options => new MessageQueue(options))
    {
    }

    /// <summary>注入队列工厂供隔离生命周期测试使用，所有权仍由管理器持有。</summary>
    internal MQManager(MQConfig config, ILogger<MQManager> logger, Func<MQConfig, IMessageQueue> queueFactory)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        ArgumentNullException.ThrowIfNull(queueFactory);
        _queueFactory = queueFactory;
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IMessageQueue MessageQueue => _messageQueue ?? throw new InvalidOperationException("消息队列管理器尚未初始化，请先调用 InitializeAsync。");

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("正在初始化消息队列系统（RabbitMQ）...");

        _config.Validate();

        // 创建消息队列实例（内部会创建 RabbitMQ 连接）
        _messageQueue = _queueFactory(_config);

        // 验证连接可用性
        _logger.LogDebug("正在验证消息队列连接...");
        try
        {
            var isHealthy = await _messageQueue.IsHealthyAsync(cancellationToken);
            if (!isHealthy)
            {
                throw new InvalidOperationException("连接已建立但状态为未打开");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息队列连接验证失败（RabbitMQ）");
            // 先分离所有权，失败清理即使抛错也不会被宿主再次释放。
            var failedQueue = _messageQueue;
            _messageQueue = null;
            IsConnected = false;
            try
            {
                await failedQueue.DisposeAsync();
            }
            catch (Exception cleanupException)
            {
                var failures = new AggregateException("消息队列初始化和失败清理均失败", ex, cleanupException);
                if (ex is OperationCanceledException cancelled)
                {
                    throw new OperationCanceledException(cancelled.Message, failures, cancelled.CancellationToken);
                }

                throw failures;
            }

            if (ex is OperationCanceledException)
            {
                throw;
            }

            throw new InvalidOperationException("消息队列连接验证失败（RabbitMQ）", ex);
        }

        IsConnected = true;
        _logger.LogDebug("消息队列系统初始化完成");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        var messageQueue = _messageQueue;
        _messageQueue = null;
        IsConnected = false;
        if (messageQueue is not null)
        {
            await messageQueue.DisposeAsync();
        }
    }
}
