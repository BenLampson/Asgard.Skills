---
name: asgard-cache
description: "通过 AsgardContext.Redis 使用对象缓存、Pipeline、条件事务、Lua、Pub/Sub、Stream、分布式锁和原生客户端，处理关闭状态、回源与事务失效。"
---

# Asgard Redis

从 8.0 起，业务服务从 `AbsAsgardContext.Redis` 获取 `IAsgardRedis`。读取状态、配置和最近异常时无需连接 Redis。默认宿主以 `caching.enabled` 为总开关，自定义宿主使用 `AddAsgardCache(config)`。

```yaml
caching:
  enabled: true
  redis:
    connectionString: "${env:REDIS_CONNECTION_STRING}"
    instanceName: "Orders:"
    database: 0
    defaultExpirationMinutes: 30
```

主 YAML 中显式写 `enabled: false` 可以覆盖环境变量。迁移旧缓存配置时应删除 `caching.memory`、`caching.redis.enabled` 和 `caching.redis.fallbackToMemoryCache`。

## 能力清单

| 框架入口 | 能力 |
| --- | --- |
| `redis.Cache` | 类型化读写、批量、命中判断、TTL、指定键与前缀失效、跨实例 `GetOrCreateAsync` |
| `redis.PipelineAsync` | 框架创建 `IBatch`，回调同步排入多条原生命令并返回任务，框架统一发送并等待所有响应。各命令独立执行 |
| `redis.TransactionAsync`、`redis.EvaluateAsync` | 带条件的 Redis 事务、自定义 Lua，键按业务命名空间处理 |
| `redis` 上的数据方法 | String、计数、Hash、Set、Sorted Set 排名、List、位图、Geo、HyperLogLog、业务键与缓存对象 TTL |
| `redis.PubSub`、`redis.SubscribePatternAsync` | 字面频道发布订阅和模式订阅 |
| `redis.Stream` | 追加、范围读取、消费组、待确认、接管、确认与裁剪，可搭配后台消费者和事务回执 |
| `redis.Locks`、`redis.SetFencedAsync` | 互斥租约、显式重入与围栏写入 |
| `redis.EnqueueInvalidation` | 在调用者的工作单元登记缓存失效；通过 `caching.outboxScope` 启用事务发件箱 |
| `redis.Data`、`redis.Streams` | 带相应前缀和逻辑库的原生 StackExchange.Redis `IDatabase`。列表搬移、位运算、地理范围搜索、更细的消费组管理等通过它们调用 |

```csharp
var redis = ctx.Redis;
await redis.PipelineAsync(batch => [
    batch.StringIncrementAsync("orders:{shop}:count"),
    batch.SetAddAsync("orders:{shop}:active", "42")
], cancellationToken);
```

回调中保留需要读取结果的命令任务，等待 `PipelineAsync` 完成后读取。需要条件提交时使用 `TransactionAsync`。Redis Cluster 多键事务和脚本使用同一哈希槽。对象缓存、`Data` 和 `Streams` 使用各自的编码或键空间。

## 禁用、异常与消费

- `redis.Status` 为 `Disabled` 时，`redis.Cache` 读取未命中、写入和删除无操作、回源直接执行工厂。消息、锁和 Pipeline 抛 `AsgardFeatureDisabledException`。
- 自定义容器只接入个别 Redis 后端时，根状态可为 `PartiallyEnabled`。对具体操作查看 `redis.PubSub.Status`、`redis.Stream.Status` 等，Pipeline 和原生命令查看 `redis.CommandStatus`。
- `redis.Configuration` 展示脱敏配置。`redis.LastError` 记录框架调用、后台 Stream 和失效投递的最近失败。调用者取得原生 `IDatabase` 后自行处理该客户端命令的异常。
- `AddAsgardRedisStreamConsumer<THandler>` 默认允许 Redis 关闭，此时消费者空闲。设置 `RedisStreamConsumerOptions.Required = true` 可让依赖 Stream 的应用在启动时检查装配。
- `caching.redis.streamDatabase` 可选，省略时与缓存同库。切换库前迁移消息和消费组。Pub/Sub 频道不按逻辑库或租户隔离。

## 事务与租户

对同一数据库和缓存范围的所有副本使用相同的 `caching.outboxScope`，使业务变更和待投递的失效记录一同提交，失败由后台重试。单实体缓存命中仍须校验实体归属。应用在授权后进入租户作用域，再访问仓储。

接口签名可查 [IAsgardRedis](references/IAsgardRedis.cs)。版本化的方法清单、事务与部署示例见 [8.0 缓存指南](https://github.com/BenLampson/Asgard/blob/v8.0.0/src/doc/17-缓存.md)。频道与消费组示例见 [Redis 消息](references/redis-messaging.md)。
