# 消息示例

## 发布消息

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <param name="cancellationToken">取消令牌。</param>
/// <returns>异步任务</returns>
public async Task {MethodName}({ParameterType} {ParameterName}, CancellationToken cancellationToken)
{
    if (AsgardContext.MessageQueue.Status == AsgardFeatureStatus.Disabled)
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

## 插件随宿主订阅

```csharp
/// <summary>当前插件拥有的订阅 ID。</summary>
private string? _subscriptionId;

/// <summary>订阅在启动时建立，业务处理使用投递上下文。</summary>
protected override async Task OnStartAsync(CancellationToken cancellationToken)
{
    var queue = GetAsgardContext().MessageQueue;
    if (queue.Status == AsgardFeatureStatus.Disabled) return;
    _subscriptionId = await queue.SubscribeAsync<{MessageType}>("{Topic}", async (message, delivery) =>
    {
        await ProcessMessageAsync(message.Value!);
        await delivery.AcknowledgeAsync();
    }, new SubscribeOptions { QueueName = "{QueueName}", AutoAck = false }, cancellationToken);
}

/// <summary>在宿主释放队列前结束本插件的订阅。</summary>
protected override async Task OnStopAsync(CancellationToken cancellationToken)
{
    if (_subscriptionId is { } id)
    {
        await GetAsgardContext().MessageQueue.UnsubscribeAsync(id, cancellationToken);
        _subscriptionId = null;
    }
}
```
