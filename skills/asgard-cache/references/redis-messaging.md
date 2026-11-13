# Redis Pub/Sub 与 Stream

本文对应当前仓库源码；接入已发布包前，先核对包是否包含 `IRedisPubSub` 和 `IRedisStream`，不要对旧版本推断有这些接口。默认宿主在 `caching.enabled: true` 时使用缓存的共享连接；自定义宿主调用 `AddAsgardCache`。`AbsAsgardContext.RedisPubSub` 和 `RedisStream` 可空，关闭缓存时为 null；直接从容器解析接口时关闭缓存会抛出 `InvalidOperationException`，不会静默丢弃消息。共享连接由宿主管理，不要手动释放。

## Pub/Sub

适合允许丢失的即时广播：消息不持久化，断线或未订阅期间不能重放；频道使用字面名称，物理名称为 `InstanceName + "pubsub:" + channel`。Redis 频道不受逻辑数据库隔离，也不自动按租户隔离，须自行制定命名和授权边界。`PublishAsync` 返回 Redis 报告的接收客户端数，不保证成功处理。每次订阅产生独立句柄，必须异步释放；取消 `ReadAsync` 只结束当次等待，不会退订。发布取消在命令发送前生效。

```csharp
// redisPubSub 为从 DI 或 Context 取得并已判空的 IRedisPubSub。
await using var subscription = await redisPubSub.SubscribeAsync("orders", cancellationToken);
await redisPubSub.PublishAsync("orders", "created", cancellationToken);
var message = await subscription.ReadAsync(cancellationToken);
```

## Stream

Stream 物理键为 `InstanceName + "stream:" + stream`，使用配置的 Redis 数据库；当前接口仅支持不重复的 UTF-8 文本字段。`AddAsync`、`ReadAsync`、`RangeAsync`、`TrimAsync` 支持追加、非阻塞读取、范围与裁剪；`CreateGroupAsync`、`ReadGroupAsync`、`PendingAsync`、`ClaimAsync`、`AcknowledgeAsync` 和 `DeleteAsync` 支持消费组及手动恢复。

```csharp
// redisStream 为从 DI 或 Context 取得并已判空的 IRedisStream。
await redisStream.CreateGroupAsync("orders", "workers", cancellationToken: cancellationToken);
await redisStream.AddAsync("orders", new Dictionary<string, string> { ["event"] = "created" }, cancellationToken);
var messages = await redisStream.ReadGroupAsync("orders", "workers", "worker-1", cancellationToken: cancellationToken);
foreach (var item in messages)
{
    await ProcessAsync(item, cancellationToken); // 业务处理应支持重复执行
    await redisStream.AcknowledgeAsync("orders", "workers", [item.Id], cancellationToken);
}
```

默认读取尚未投递的新消息；读取指定消费者待确认消息可传 `position: "0"`。消费失败时按业务策略用 `PendingAsync` 查找并用 `ClaimAsync` 转移超时消息，成功处理后确认。API 不提供阻塞读取、自动确认、自动重试或后台消费；删除或裁剪消息不自动确认待处理项。Stream 存在 Redis 中，重启后的持久性取决于 Redis 持久化配置。取消等待不代表已发往 Redis 的命令撤销，业务应处理重复和结果未知。

## 缓存删除边界

包含本次保护的源码在单键、批量和前缀删除时以 Redis 单键脚本原子检查类型，跳过 Stream，同时保留现有缓存与 Stream 的物理键。前缀扫描整体仍非原子；旧版本实例不具备该保护，滚动升级时不能依赖全部实例已隔离。缓存和 Stream 若指向同一个物理键，则无法同时占用该键，读写会因类型不匹配失败；应使用不同名字。租户和应用隔离仍须业务按名称、配置及授权建立。
