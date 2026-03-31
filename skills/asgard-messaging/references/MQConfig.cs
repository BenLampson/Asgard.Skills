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
    /// 获取或设置选定的消息队列提供商。
    /// </summary>
    [ConfigPath("messaging.provider", DefaultValue = MQProvider.RabbitMQ)]
    public MQProvider Provider { get; set; } = MQProvider.RabbitMQ;

    /// <summary>
    /// 获取或设置 RabbitMQ 特定选项。
    /// </summary>
    [ConfigPath("messaging.rabbitmq")]
    public RabbitMQOptions RabbitMQ { get; set; } = new();

    /// <summary>
    /// 获取或设置 Kafka 特定选项。
    /// </summary>
    [ConfigPath("messaging.kafka")]
    public KafkaOptions Kafka { get; set; } = new();

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
    ///   <item><description>RabbitMQ、Kafka、Tracing、Retry 和 DelayedMessage 配置对象不为 null</description></item>
    ///   <item><description>Tracing、Retry 和 DelayedMessage 配置有效</description></item>
    ///   <item><description>提供商特定选项有效</description></item>
    /// </list>
    /// 注意：仅当消息队列模块启用时（Enabled = true）才执行提供商特定验证。
    /// </remarks>
    public void Validate()
    {
        if (RabbitMQ is null)
        {
            throw new InvalidOperationException(MessagingErrorMessages.RabbitMQConfigCannotBeNull);
        }

        if (Kafka is null)
        {
            throw new InvalidOperationException(MessagingErrorMessages.KafkaConfigCannotBeNull);
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

        // 仅当模块启用时验证提供商特定选项
        if (Enabled)
        {
            switch (Provider)
            {
                case MQProvider.RabbitMQ:
                    ValidateRabbitMQOptions();
                    break;
                case MQProvider.Kafka:
                    ValidateKafkaOptions();
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported MQ provider: {Provider}");
            }
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

    private void ValidateKafkaOptions()
    {
        if (!Kafka.Enabled)
        {
            throw new InvalidOperationException("提供商是 Kafka，但 Kafka 已被禁用。");
        }

        if (string.IsNullOrWhiteSpace(Kafka.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka BootstrapServers 是必需的。");
        }

        if (string.IsNullOrWhiteSpace(Kafka.GroupId))
        {
            throw new InvalidOperationException("Kafka GroupId 是必需的。");
        }

        if (Kafka.SessionTimeoutMs <= 0)
        {
            throw new InvalidOperationException("Kafka SessionTimeoutMs 必须大于 0。");
        }

        if (Kafka.MaxPollIntervalMs <= 0)
        {
            throw new InvalidOperationException("Kafka MaxPollIntervalMs 必须大于 0。");
        }

        if (Kafka.MaxPollRecords <= 0)
        {
            throw new InvalidOperationException("Kafka MaxPollRecords 必须大于 0。");
        }

        if (Kafka.Acks is not (0 or 1 or -1))
        {
            throw new InvalidOperationException("Kafka Acks 必须是 0、1 或 -1。");
        }

        if (Kafka.Retries < 0)
        {
            throw new InvalidOperationException("Kafka 重试次数不能为负数。");
        }

        if (Kafka.NumPartitions <= 0)
        {
            throw new InvalidOperationException("Kafka NumPartitions 必须大于 0。");
        }

        if (Kafka.ReplicationFactor <= 0)
        {
            throw new InvalidOperationException("Kafka ReplicationFactor 必须大于 0。");
        }
    }
}
