---
name: asgard-messaging
description: Asgard 消息队列模块 skill。Use when configuring or using messaging.enabled, MQConfig, RabbitMQ, Kafka, tracing, retry, delayed messages, dead-letter handling, message publishing, subscription handlers, or message processing through AbsAsgardContext.
---

# Asgard Messaging

## 作用

用于配置和使用 Asgard 消息队列模块。支持 RabbitMQ 和 Kafka 两种提供者，提供统一的发布订阅抽象，支持重试、延迟消息、死信队列、消息追踪等高级特性。

## 什么时候使用

- **需要启用消息队列** - 在配置文件中配置 `messaging.enabled` 和选择提供者
- **需要发布消息** - 通过 `AbsAsgardContext.MessageQueue` 发布消息到指定主题
- **需要订阅消息** - 实现消费者处理传入消息
- **需要动态操作消息队列** - 在运行时通过接口发布/取消订阅
- **需要理解配置项** - 解释 RabbitMQ/Kafka 不同配置项的含义

## 配置约定

### 完整配置结构

```yaml
messaging:
  enabled: {Enabled}
  provider: {Provider} # RabbitMQ or Kafka
  rabbitmq:
    enabled: {RabbitMQEnabled}
    hostName: "{HostName}"
    port: {Port}
    userName: "{UserName}"
    password: "{Password}"
    virtualHost: "{VirtualHost}"
    requestedHeartbeat: {RequestedHeartbeat}
    requestedConnectionTimeout: {RequestedConnectionTimeout}
    retryCount: {RetryCount}
    retryIntervalMilliseconds: {RetryIntervalMilliseconds}
  kafka:
    enabled: {KafkaEnabled}
    bootstrapServers: "{BootstrapServers}"
    groupId: "{GroupId}"
    acks: {Acks} # 0, 1, or -1
    retries: {Retries}
    numPartitions: {NumPartitions}
    replicationFactor: {ReplicationFactor}
    sessionTimeoutMs: {SessionTimeoutMs}
    maxPollIntervalMs: {MaxPollIntervalMs}
    maxPollRecords: {MaxPollRecords}
  tracing:
    enabled: {TracingEnabled}
  retry:
    maxRetryCount: {MaxRetryCount}
    initialDelayMilliseconds: {InitialDelayMilliseconds}
  delayedMessage:
    enabled: {DelayedMessageEnabled}
    exchangeName: "{DelayedExchangeName}"
  enableDeadLetterQueue: {EnableDeadLetterQueue}
  deadLetterQueueSuffix: "{DeadLetterQueueSuffix}"
```

### 提供者选择

| 提供者 | 说明 |
|--------|------|
| `RabbitMQ` | 适合大多数场景，消息路由灵活 |
| `Kafka` | 适合高吞吐量、日志流场景 |

### 高级特性

| 特性 | 说明 | 默认 |
|------|------|------|
| `tracing.enabled` | 启用消息追踪 | `false` |
| `enableDeadLetterQueue` | 启用死信队列，处理失败消息 | `true` |
| `delayedMessage.enabled` | 支持延迟消息 | `false` |

## 使用方式

通过 `AbsAsgardContext.MessageQueue` 获取消息队列能力，该属性可为 `null`（模块未启用时），必须做空检查。

### 常用操作

| 操作 | 方法签名 | 说明 |
|------|----------|------|
| 发布消息 | `PublishAsync<T>(topic, message, options)` | 发布消息到指定主题 |
| 订阅消息 | `SubscribeAsync<T>(topic, handler, options)` | 订阅指定主题消息，返回订阅标识 |
| 取消订阅 | `UnsubscribeAsync(subscriptionId)` | 取消订阅并释放资源 |
| 批量发布 | `PublishBatchAsync<T>(topic, messages)` | 批量发布消息提高吞吐量 |

## 代码示例

### 发布消息

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <returns>异步任务</returns>
public async Task {MethodName}({ParameterType} {ParameterName})
{
    if (AsgardContext.MessageQueue == null)
    {
        // 消息队列未启用，降级处理
        {FallbackCode}
        return;
    }

    await AsgardContext.MessageQueue.PublishAsync(
        "{Topic}",
        {Message},
        new PublishOptions
        {
            Key = {Key},
            Headers = new Dictionary<string, string>
            {
                ["{HeaderKey}"] = "{HeaderValue}"
            }
        },
        cancellationToken);
}
```

### 订阅消息（插件初始化中）

```csharp
/// <summary>
/// 初始化消息订阅
/// </summary>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>订阅标识</returns>
public override async Task InitializeAsync(CancellationToken cancellationToken)
{
    await base.InitializeAsync(cancellationToken);

    if (AsgardContext.MessageQueue != null)
    {
        _ = await AsgardContext.MessageQueue.SubscribeAsync<{MessageType}>(
            "{Topic}",
            async (message, context) =>
            {
                await ProcessMessageAsync(message.Value);
                await context.AcknowledgeAsync();
            },
            new SubscribeOptions
            {
                AutoAck = false,
                QueueName = "{QueueName}"
            },
            cancellationToken);
    }
}

/// <summary>
/// 处理接收的消息
/// </summary>
/// <param name="message">消息实例</param>
/// <returns>异步任务</returns>
private async Task ProcessMessageAsync({MessageType} message)
{
    try
    {
        {ProcessingLogic}
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理消息 {Topic} 发生异常", "{Topic}");
        // 重试由框架处理，这里只记录日志
        throw;
    }
}
```

## 推荐做法

- 只选择一个提供者，不要同时配置两套导致混淆
- topic / queue 名称保持稳定，不要随机变化
- 消费者逻辑保持简洁，复杂业务下沉到服务层
- 总是调用 `context.AcknowledgeAsync()` 确认消息处理完成
- 处理异常后正常抛出，让框架负责重试和死信路由
- 访问 `AbsgardContext.MessageQueue` 总是先做空检查，支持模块动态禁用

## 不要这样做

❌ 不要同时启用 RabbitMQ 和 Kafka 两套配置，选择一个就好

❌ 不要忽略 `MessageQueue` 可能为 null，模块可以被禁用

❌ 不要在消费者 handler 编写大段业务逻辑，委托给服务层保持简洁

❌ 不要忘记确认消息处理，不确认会导致消息一直锁定

❌ 不要吃掉异常不抛出，让框架无法进行重试和死信处理

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `MQConfig.cs` - 消息队列配置类
- `MQManager.cs` - 消息队列管理器
- `IMessageQueue.cs` - 消息队列核心接口

代码范本请参考 `templates/` 目录：
- `appsettings.yaml.template` - 配置文件范本
- `PublishMessage.cs.template` - 发布消息范本
- `SubscribeMessage.cs.template` - 订阅消息范本
