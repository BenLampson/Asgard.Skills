---
name: asgard-distributed-lock
description: Asgard Redis 分布式锁 skill。Use when configuring, registering, using, or debugging IDistributedLock, DistributedLockOptions, DistributedLockAcquireOptions, automatic renewal, LockLostToken, owner-token-safe release, Yggdrasil automatic registration, or Redis-backed multi-instance mutual exclusion in Asgard.
---

# Asgard Distributed Lock

## 核心契约

- 把分布式锁视为 Redis 可用时的宿主基础能力，不要求业务插件显式注册。
- 使用 Yggdrasil 时，只要 `caching.enabled` 与 `caching.redis.enabled` 同时为 `true`，宿主就自动注册 `IDistributedLock` 并注入 `AbsAsgardContext.DistributedLock`。
- 不要添加 `distributedLock.enabled`。`distributedLock` 配置节只覆盖行为参数，可以完全省略。
- 使用独立 DI 容器而非 Yggdrasil 时，先注册启用 Redis 的 `CacheConfig`，再调用无参 `services.AddDistributedLock()`。
- 长时间持锁的业务必须监听 `IDistributedLockHandle.LockLostToken`，不能只依赖自动续租。

需要完整配置/API 表和边界语义时，读取 [references/distributed-lock-contract.md](references/distributed-lock-contract.md)。

## 配置

最小配置只需要启用 Redis：

```yaml
caching:
  enabled: true
  redis:
    enabled: true
    connectionString: "127.0.0.1:6379"
```

以下配置节全部可省略；省略时直接使用框架默认值：

```yaml
distributedLock:
  keyPrefix: "lock:"
  defaultLeaseTime: "00:00:30"
  defaultAcquireTimeout: "00:00:05"
  retryInterval: "00:00:00.200"
  autoRenewal: true
```

时间参数不能小于 1 毫秒。Redis 连接、数据库编号和实例前缀始终复用 `caching.redis`，不要在 `distributedLock` 下重复定义连接。

## 获取与释放

立即竞争、拿不到就退出时使用 `TryAcquireAsync`：

```csharp
await using var handle = await AsgardContext.DistributedLock.TryAcquireAsync(
    $"jobs:{jobName}",
    cancellationToken: cancellationToken);

if (handle is null)
{
    return;
}

using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
    cancellationToken,
    handle.LockLostToken);

await ExecuteCoreAsync(operationCancellation.Token);
```

需要在限定时间内轮询等待时使用 `AcquireAsync`：

```csharp
await using var handle = await distributedLock.AcquireAsync(
    $"orders:{orderId}",
    new DistributedLockAcquireOptions
    {
        AcquireTimeout = TimeSpan.FromSeconds(3),
        RetryInterval = TimeSpan.FromMilliseconds(100)
    },
    cancellationToken);
```

- 始终使用 `await using`，即使业务异常也要尽快释放。
- `TryAcquireAsync` 竞争失败返回 `null`；`AcquireAsync` 超时抛出 `TimeoutException`。
- 自动续租默认开启，周期约为当前租期的三分之一；可通过单次 `AutoRenewal = false` 覆盖。
- 释放与续租都校验 owner token，旧句柄不能误删或误续租其他持有者的锁。
- `LockLostToken` 的取消通知异步派发；业务必须让受保护操作主动响应该令牌。

## 注册边界

### Yggdrasil 宿主

不要在插件的 `AddXxx()`、`Program.cs` 或 starter 中重复调用 `AddDistributedLock()`。宿主会按 Redis 配置自动装配，并复用缓存模块建立的 `IConnectionMultiplexer`。

### 独立容器

```csharp
services.AddSingleton(cacheConfig);
services.AddDistributedLock();
```

无参注册已经包含完整默认值。只有独立容器或明确需要代码级覆盖时才使用配置委托：

```csharp
services.AddDistributedLock(options =>
{
    options.KeyPrefix = "billing-lock:";
    options.DefaultLeaseTime = TimeSpan.FromMinutes(1);
});
```

显式调用会接管锁配置；在 Yggdrasil 链路中保留旧的显式无参注册，可能屏蔽 YAML 中的 `distributedLock` 覆盖，因此应删除重复注册。

## 能力选择

- 业务服务已经依赖 `AbsAsgardContext` 时，优先使用 `AsgardContext.DistributedLock` 并做空检查。
- 只需要锁能力、希望依赖更明确时，可以直接注入 `IDistributedLock`。
- 需要缓存配置与行为时转到 `$asgard-cache`。
- 需要宿主启动和服务注册顺序时转到 `$asgard-host-project`。
- 需要 Context 生命周期与可空能力语义时转到 `$asgard-context-usage`。

## 不要这样做

- 不要把 Redis 分布式锁描述成金融级强一致或可重入锁。
- 不要使用锁保护无法响应取消的无限长任务。
- 不要忽略 `LockLostToken` 后继续提交受保护写操作。
- 不要手写 `SET NX` 和无 owner token 校验的 `DEL`。
- 不要把缓存启用等同于 Redis 启用；自动装配要求缓存总开关与 Redis 子开关同时开启。
- 不要通过已废弃的 `DistributedLockOptions.Enabled` 控制注册；该属性只为 5.1.x API 兼容保留，运行时忽略。
