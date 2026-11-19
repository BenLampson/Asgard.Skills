namespace Asgard.Core.Messaging;

/// <summary>
/// 消息队列管理器实现，负责消息队列系统的初始化、连接验证和生命周期管理。
/// </summary>
/// <remarks>
/// <para>
/// 宿主把最终注册的队列交给管理器验证。独立运行时管理器创建 RabbitMQ 连接并通过健康检查验证可用性。
/// </para>
/// </remarks>
public sealed class MQManager : IMQManager
{
    private readonly MQConfig _config;
    private readonly ILogger<MQManager> _logger;
    /// <summary>串行化队列验证和释放，避免并发调用转移同一资源的所有权。</summary>
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private volatile IMessageQueue? _messageQueue;
    private readonly Func<MQConfig, IMessageQueue> _queueFactory;
    private bool _disposed;
    private IMessageQueue? _hostQueue;
    private bool _ownsQueue = true;

    /// <summary>选择最终注册的后端，独立使用的管理器继续拥有自己创建的队列。</summary>
    internal void UseQueue(IMessageQueue queue, bool ownsQueue)
    {
        _hostQueue = queue;
        _ownsQueue = ownsQueue;
    }

    /// <summary>
    /// 初始化 <see cref="MQManager"/> 类的新实例。
    /// </summary>
    /// <param name="config">消息队列配置。</param>
    /// <param name="logger">日志器。</param>
    public MQManager(MQConfig config, ILogger<MQManager> logger)
        : this(config, logger, options => new MessageQueue(options, logger))
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
    public bool IsConnected => _messageQueue is not null;

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_messageQueue is not null) return;

            _logger.LogDebug("正在初始化消息队列系统（RabbitMQ）...");
            _config.Validate();
            _messageQueue = await CreateHealthyQueueAsync(cancellationToken);
            _logger.LogDebug("消息队列系统初始化完成");
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }

    /// <summary>候选队列在验证通过前仅由本次初始化持有，验证失败时由创建方释放。</summary>
    private async Task<IMessageQueue> CreateHealthyQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("正在验证消息队列连接...");
        var queue = _hostQueue ?? _queueFactory(_config);
        try
        {
            if (!await queue.IsHealthyAsync(cancellationToken))
            {
                throw new InvalidOperationException("连接已建立但状态为未打开");
            }
            return queue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息队列连接验证失败（RabbitMQ）");
            try
            {
                if (_ownsQueue) await queue.DisposeAsync();
                _hostQueue = null;
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
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            if (_disposed) return;
            _disposed = true;
            var queue = _messageQueue ?? _hostQueue;
            _messageQueue = null;
            _hostQueue = null;
            if (_ownsQueue && queue is not null) await queue.DisposeAsync();
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }
}
