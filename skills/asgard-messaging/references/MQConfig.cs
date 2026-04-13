namespace Asgard.Abstractions.Messaging;

/// <summary>
/// 消息队列配置根类。
/// </summary>
public class MQConfig : ISystemConfig
{
    /// <summary>
    /// 获取或设置是否启用消息队列模块。
    /// </summary>
    /// <remarks>
    /// 当设置为 false 时，整个消息队列模块将被禁用，不会注册任何消息队列服务。
    /// 默认值为 false。
    /// </remarks>
    /// <value>true 表示启用消息队列模块；false 表示禁用。</value>
    [ConfigPath("messaging.enabled", DefaultValue = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 获取或设置 RabbitMQ 特定选项。
    /// </summary>
    [ConfigPath("messaging.rabbitmq")]
    public RabbitMQOptions RabbitMQ { get; set; } = new();

    /// <summary>
    /// 获取或设置消息追踪选项。
    /// </summary>
    [ConfigPath("messaging.tracing")]
    public TracingOptions Tracing { get; set; } = new();

    /// <summary>
    /// 获取或设置消息处理使用的重试选项。
    /// </summary>
    [ConfigPath("messaging.retry")]
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// 获取或设置延迟消息选项。
    /// </summary>
    [ConfigPath("messaging.delayedMessage")]
    public DelayedMessageOptions DelayedMessage { get; set; } = new();

    /// <summary>
    /// 获取或设置是否启用死信队列支持。
    /// </summary>
    [ConfigPath("messaging.enableDeadLetterQueue", DefaultValue = true)]
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// 获取或设置附加到死信队列名称的后缀。
    /// </summary>
    [ConfigPath("messaging.deadLetterQueueSuffix", DefaultValue = ".dlq")]
    public string DeadLetterQueueSuffix { get; set; } = ".dlq";

    /// <summary>
    /// 获取或设置默认重试次数。
    /// </summary>
    public int DefaultRetryCount
    {
        get => Retry.MaxRetryCount;
        set => Retry.MaxRetryCount = value;
    }

    /// <summary>
    /// 获取或设置默认重试间隔（毫秒）。
    /// </summary>
    public int DefaultRetryIntervalMilliseconds
    {
        get => Retry.InitialDelayMilliseconds;
        set => Retry.InitialDelayMilliseconds = value;
    }

    /// <summary>
    /// 获取或设置是否启用消息追踪。
    /// </summary>
    public bool EnableMessageTracing
    {
        get => Tracing.Enabled;
        set => Tracing.Enabled = value;
    }

    /// <summary>
    /// 验证配置值和提供商特定约束。
    /// </summary>
    /// <remarks>
    /// 此方法在配置加载后自动调用。它验证以下内容：
    /// <list type="number">
    ///   <item><description>RabbitMQ、Tracing、Retry 和 DelayedMessage 配置对象不为 null</description></item>
    ///   <item><description>Tracing、Retry 和 DelayedMessage 配置有效</description></item>
    ///   <item><description>RabbitMQ 相关选项有效</description></item>
    /// </list>
    /// 注意：仅当消息队列模块启用时（Enabled = true）才执行 RabbitMQ 选项验证。
    /// </remarks>
    public void Validate()
    {
        if (RabbitMQ is null)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQConfigCannotBeNull);
        }

        if (Tracing is null)
        {
            throw new InvalidOperationException(MessagingErrorMessages.TracingConfigCannotBeNull);
        }

        if (Retry is null)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RetryConfigCannotBeNull);
        }

        if (DelayedMessage is null)
        {
            throw new InvalidOperationException(MessagingErrorMessages.DelayedMessageConfigCannotBeNull);
        }

        Tracing.Validate();
        Retry.Validate();
        DelayedMessage.Validate();

        // 仅在模块启用时验证 RabbitMQ 参数，避免纯声明配置阻塞宿主启动。
        if (Enabled)
        {
            ValidateRabbitMQOptions();
        }
    }

    private void ValidateRabbitMQOptions()
    {
        if (!RabbitMQ.Enabled)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQDisabledButProviderIsRabbitMQ);
        }

        if (string.IsNullOrWhiteSpace(RabbitMQ.HostName))
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQHostNameRequired);
        }

        if (RabbitMQ.Port <= 0 || RabbitMQ.Port > 65535)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQPortMustBeInRange);
        }

        if (string.IsNullOrWhiteSpace(RabbitMQ.UserName))
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQUsernameRequired);
        }

        if (RabbitMQ.RequestedHeartbeat > 0 && RabbitMQ.RequestedHeartbeat < 10)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQHeartbeatCannotBeLessThan10);
        }

        if (RabbitMQ.RequestedConnectionTimeout <= 0)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQConnectionTimeoutMustBeGreaterThanZero);
        }

        if (RabbitMQ.RetryCount < 0)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQRetryCountCannotBeNegative);
        }

        if (RabbitMQ.RetryIntervalMilliseconds <= 0)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQRetryIntervalMustBeGreaterThanZero);
        }
    }
}
