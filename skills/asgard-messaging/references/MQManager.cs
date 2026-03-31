namespace Asgard.Core.Messaging;

/// <summary>
/// 消息队列管理器实现，负责消息队列系统的初始化、连接验证和生命周期管理。
/// </summary>
/// <remarks>
/// <para>
/// 此类独立于 IoC 容器，在系统启动阶段（Phase 3）完成初始化和连接验证。
/// 初始化时会创建 RabbitMQ/Kafka 连接，并通过健康检查验证可用性。
/// </para>
/// </remarks>
public sealed class MQManager : IMQManager
{
    private readonly MQConfig _config;
    private readonly ILogger<MQManager> _logger;
    private MessageQueue? _messageQueue;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="MQManager"/> 类的新实例。
    /// </summary>
    /// <param name="config">消息队列配置。</param>
    /// <param name="logger">日志器。</param>
    public MQManager(MQConfig config, ILogger<MQManager> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

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
        _logger.LogDebug("正在初始化消息队列系统（Provider: {Provider}）...", _config.Provider);

        _config.Validate();

        // 创建消息队列实例（内部会创建 RabbitMQ/Kafka 连接）
        _messageQueue = new MessageQueue(_config);

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
            _logger.LogError(ex, "消息队列连接验证失败（Provider: {Provider}）", _config.Provider);
            await _messageQueue.DisposeAsync();
            _messageQueue = null;
            throw new InvalidOperationException($"消息队列连接验证失败（Provider: {_config.Provider}）", ex);
        }

        IsConnected = true;
        _logger.LogDebug("消息队列系统初始化完成");
    }

    /// <inheritdoc/>
    public async Value DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_messageQueue != null)
        {
            await _messageQueue.DisposeAsync();
        }

        IsConnected = false;
    }
}
