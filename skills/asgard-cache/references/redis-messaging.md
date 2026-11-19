# Redis Pub/Sub 与 Stream

Asgard 8.0 的入口为 `ctx.Redis.PubSub` 与 `ctx.Redis.Stream`。默认宿主共享连接，自定义宿主可用 `AddAsgardCache`。关闭时，依赖 Redis 的业务调用抛 `AsgardFeatureDisabledException`。将后台消费者设为 `Required = true` 即可要求它在启动时验证 Stream。业务只释放自己取得的订阅句柄。

## Pub/Sub

适合允许丢失的即时广播：消息不持久化，断线或未订阅期间不能重放；频道使用字面名称，物理名称为 `InstanceName + "pubsub:" + channel`。Redis 频道不受逻辑数据库隔离，也不自动按租户隔离，须自行制定命名和授权边界。`PublishAsync` 返回 Redis 报告的接收客户端数，不保证成功处理。每次订阅产生独立句柄，必须异步释放；取消 `ReadAsync` 只结束当次等待，不会退订。发布取消在命令发送前生效。

```csharp
var redisPubSub = redis.PubSub;
await using var subscription = await redisPubSub.SubscribeAsync("orders", cancellationToken);
await redisPubSub.PublishAsync("orders", "created", cancellationToken);
var message = await subscription.ReadAsync(cancellationToken);
```

## Stream

Stream 物理键为 `InstanceName + "stream:" + stream`，使用 `caching.redis.streamDatabase`，省略时沿用 `caching.redis.database`；当前接口仅支持不重复的 UTF-8 文本字段。`AddAsync`、`ReadAsync`、`RangeAsync`、`TrimAsync` 支持追加、非阻塞读取、范围与裁剪；`CreateGroupAsync`、`ReadGroupAsync`、`PendingAsync`、`ClaimAsync`、`AcknowledgeAsync` 和 `DeleteAsync` 支持消费组及手动恢复。

```csharp
var redisStream = redis.Stream;
await redisStream.CreateGroupAsync("orders", "workers", cancellationToken: cancellationToken);
await redisStream.AddAsync("orders", new Dictionary<string, string> { ["event"] = "created" }, cancellationToken);
var messages = await redisStream.ReadGroupAsync("orders", "workers", "worker-1", cancellationToken: cancellationToken);
foreach (var item in messages)
{
    await ProcessAsync(item, cancellationToken); // 业务处理应支持重复执行
    await redisStream.AcknowledgeAsync("orders", "workers", [item.Id], cancellationToken);
}
```

默认读取尚未投递的新消息；读取指定消费者待确认消息可传 `position: "0"`。消费失败时按业务策略用 `PendingAsync` 查找并用 `ClaimAsync` 转移超时消息，成功处理后确认。`IRedisStream` 使用非阻塞读取和手动确认。删除或裁剪消息后应检查待处理项。Stream 存在 Redis 中，重启后的持久性取决于 Redis 持久化配置。取消等待不代表已发往 Redis 的命令撤销，业务应处理重复和结果未知。

## 后台消费与去重

使用 `AddAsgardRedisStreamConsumer<THandler>` 注册后台处理器，可配置源流、组、死信流、可见性超时和最大投递次数。后台消费者负责续租、接管、退避重试和确认。数据库副作用可通过 `TransactionalRedisStreamHandler` 与回执放在同一事务。外部调用使用消息携带的稳定幂等键。示例见 [缓存指南](https://github.com/BenLampson/Asgard/blob/v8.0.0/src/doc/17-缓存.md#stream-后台消费者)。

模式订阅使用 `AsgardRedis.SubscribePatternAsync`。队列提供实际频道名，使用结束后调用 `UnsubscribeAsync()`。

## 切换 Stream 数据库

`streamDatabase` 可设置为 0–15，实际可用索引由 Redis 服务端决定。设置为不同的逻辑库仍使用同一连接；宿主初始化时探测该库。仅支持 DB 0 的部署应保持同库。Pub/Sub 频道、缓存和分布式锁不受这个配置影响。

切换配置不会搬迁消息、消费组或待确认状态。先排空旧库或迁移数据，再统一切换生产者与消费者。

## 缓存删除边界

包含本次保护的源码在单键、批量和前缀删除时以 Redis 单键脚本原子检查类型，跳过 Stream，同时保留现有缓存与 Stream 的物理键。前缀扫描整体仍非原子；旧版本实例不具备该保护，滚动升级时不能依赖全部实例已隔离。不同逻辑库允许同名键，缓存删除只访问缓存库。同库时缓存和 Stream 不能占用同一个物理键，应使用不同名字。租户和应用隔离仍须业务按名称、配置及授权建立。
