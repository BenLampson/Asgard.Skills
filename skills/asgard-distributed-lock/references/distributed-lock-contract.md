# Distributed Lock Contract

## 配置表

| 配置路径 | 类型 | 默认值 | 说明 |
|---|---|---:|---|
| `distributedLock.keyPrefix` | `string` | `lock:` | 锁键前缀，拼接在 Redis `instanceName` 后 |
| `distributedLock.defaultLeaseTime` | `TimeSpan` | 30 秒 | 初始租期和每次续租后的完整租期 |
| `distributedLock.defaultAcquireTimeout` | `TimeSpan` | 5 秒 | `AcquireAsync` 默认等待上限 |
| `distributedLock.retryInterval` | `TimeSpan` | 200 毫秒 | 等待获取时的轮询间隔 |
| `distributedLock.autoRenewal` | `bool` | `true` | 是否默认自动续租 |

配置节没有 `enabled` 字段。所有时间值必须至少为 1 毫秒。

## 单次获取参数

`DistributedLockAcquireOptions` 的所有属性都可空；空值回退到全局默认值：

| 属性 | 作用 |
|---|---|
| `LeaseTime` | 覆盖当前句柄租期 |
| `AcquireTimeout` | 覆盖 `AcquireAsync` 等待上限 |
| `RetryInterval` | 覆盖当前等待轮询间隔 |
| `AutoRenewal` | 覆盖当前句柄自动续租行为 |

## 句柄契约

`IDistributedLockHandle` 暴露：

- `Key`：业务传入的原始键。
- `OwnerToken`：本次持有者唯一令牌。
- `LeaseTime`：本次租期。
- `AcquiredAt`：成功获取时间。
- `LockLostToken`：所有权终止通知。
- `ReleaseAsync()`：仅在 owner token 仍匹配时释放。

`LockLostToken` 会在自动续租失败、租期到期、所有权被替换或句柄释放时异步取消。取消回调异常不会破坏锁内部清理流程。

## Redis 原子性

- 获取：`SET key ownerToken NX PX lease`。
- 释放：Lua 内先比较 owner token，再执行 `DEL`。
- 续租：Lua 内先比较 owner token，再执行 `PEXPIRE`。

该模型避免过期旧句柄误删或误续租后来持有者的锁，但不提供跨多个 Redis 主节点的共识协议，不应宣传为金融级强一致。

## 生命周期与失败行为

- 取消或 Redis 瞬时错误导致释放失败时，句柄允许再次调用 `ReleaseAsync()`。
- 调用取消或等待超时后若 Redis 命令晚到成功，框架会尝试按 owner token 清理；清理仍失败时由 TTL 兜底。
- 自动续租失败即按锁已丢失处理，不继续假设互斥权有效。
- 同一句柄并发释放只会有一个调用真正删除锁。
- 宿主共享 Redis 连接由缓存管理器拥有；锁服务只释放自己独立创建的连接。

## 版本兼容

- `DistributedLockOptions.Enabled` 仅用于保持 5.1.x 源码兼容，已废弃且不参与运行时注册判断。
- `IDistributedLockHandle.LockLostToken` 提供默认接口实现，旧的外部句柄实现无需立即增加成员即可继续编译。
- 旧的 `RedisDistributedLock` 与 `RedisDistributedLockHandle` 构造函数保留转发重载。
