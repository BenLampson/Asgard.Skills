---
name: asgard-messaging
description: Asgard 消息队列模块 skill。Use when configuring or using messaging.enabled, MQConfig, RabbitMQ, tracing, retry, delayed messages, dead-letter handling, message publishing, subscription handlers, or message processing through AbsAsgardContext.
---

# Asgard Messaging

## 作用

用于配置和使用 Asgard 消息队列模块。当前消息模块统一基于 RabbitMQ，提供统一的发布订阅抽象，并支持重试、延迟消息、死信队列、消息追踪等能力。

## 什么时候使用

- **需要启用消息队列** - 在项目根目录 `app.yaml` 中配置 `messaging.enabled` 与 `messaging.rabbitmq.*`
- **需要发布消息** - 通过 `AbsAsgardContext.MessageQueue` 发布消息到指定主题
- **需要订阅消息** - 实现消费者处理传入消息
- **需要动态操作消息队列** - 在运行时通过接口发布、订阅与取消订阅
- **需要理解配置项** - 解释 RabbitMQ、追踪、重试、延迟消息与死信配置

## 配置约定

### 完整配置结构

```yaml
messaging:
  enabled: {Enabled}
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

### 高级特性

- `tracing.enabled`：启用消息追踪，默认 `false`
- `enableDeadLetterQueue`：启用死信队列，默认 `true`
- `delayedMessage.enabled`：启用延迟消息，默认 `false`

## 使用方式

通过 `AbsAsgardContext.MessageQueue` 获取消息队列能力，该属性可为 `null`（模块未启用时），必须做空检查。

### 常用操作

| 操作 | 方法签名 | 说明 |
|------|----------|------|
| 发布消息 | `PublishAsync<T>(topic, message, options)` | 发布消息到指定主题 |
| 订阅消息 | `SubscribeAsync<T>(topic, handler, options)` | 订阅指定主题消息，返回订阅标识 |
| 取消订阅 | `UnsubscribeAsync(subscriptionId)` | 取消订阅并释放资源 |
| 批量发布 | `PublishBatchAsync<T>(topic, messages)` | 批量发布消息 |

## 代码示例

### 发布消息

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>异步任务</returns>
public async Task {MethodName}Async(
    {ParameterType} {ParameterName},
    CancellationToken cancellationToken = default)
{
    if (AsgardContext.MessageQueue is null)
    {
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

### 订阅消息

```csharp
/// <summary>
/// 初始化消息订阅。
/// </summary>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>异步任务</returns>
public override async Task InitializeAsync(CancellationToken cancellationToken)
{
    await base.InitializeAsync(cancellationToken);

    if (AsgardContext.MessageQueue is null)
    {
        return;
    }

    _ = await AsgardContext.MessageQueue.SubscribeAsync<{MessageType}>(
        "{Topic}",
        async (message, context) =>
        {
            await ProcessMessageAsync(message.Value!, cancellationToken);
            await context.AcknowledgeAsync();
        },
        new SubscribeOptions
        {
            AutoAck = false
        },
        cancellationToken);
}

/// <summary>
/// 处理接收的消息。
/// </summary>
/// <param name="message">消息实例</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>异步任务</returns>
private async Task ProcessMessageAsync(
    {MessageType} message,
    CancellationToken cancellationToken)
{
    try
    {
        {ProcessingLogic}
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理消息 {Topic} 发生异常", "{Topic}");
        throw;
    }
}
```

## 推荐做法

- 统一维护 `messaging.rabbitmq.*` 配置，不要再保留旧的 provider 切换思路
- topic / queue 名称保持稳定，不要随机变化
- 消费者逻辑保持简洁，复杂业务下沉到服务层
- 总是调用 `context.AcknowledgeAsync()` 确认消息处理完成
- 处理异常后正常抛出，让框架负责重试和死信路由
- 访问 `AbsAsgardContext.MessageQueue` 前先做空检查，支持模块动态禁用

## 不要这样做

❌ 不要继续沿用已经移除的旧配置字段，统一使用当前的 RabbitMQ 配置结构

❌ 不要忽略 `MessageQueue` 可能为 `null`

❌ 不要在消费者 handler 中编写大段业务逻辑，委托给服务层保持简洁

❌ 不要忘记确认消息处理，不确认会导致消息一直处于未完成状态

❌ 不要吃掉异常不抛出，让框架无法进行重试和死信处理

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `MQConfig.cs` - 消息队列配置类
- `MQManager.cs` - 消息队列管理器
- `IMessageQueue.cs` - 消息队列核心接口

代码范本请参考 `templates/` 目录：
- `app.yaml.template` - 配置片段范本，合并到项目根目录 `app.yaml`
- `PublishMessage.cs.template` - 发布消息范本
- `SubscribeMessage.cs.template` - 订阅消息范本
