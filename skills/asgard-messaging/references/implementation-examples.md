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
