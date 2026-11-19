# Context 公共能力与缺省行为

表中 `ctx` 是当前 DI 作用域中的 `AbsAsgardContext`。Yggdrasil 一列指可正常构建的最小宿主，省略可选模块的 YAML，应用未另行注册服务。空容器一列指只调用 `new ServiceCollection().AddAsgardContext()`，没有其他注册。两种容器的根入口和子入口始终可取得。

## Asgard 8.1

| 规范根入口 | 最小 Yggdrasil | 空容器 | 业务用途及缺省行为 |
| --- | --- | --- | --- |
| `Redis` | Disabled | Disabled | `Cache` 读未命中，写入、删除为空操作，`GetOrCreateAsync` 仍执行回源。命令、Pipeline、事务、Lua、Pub/Sub、Stream、锁需要各自能力启用。 |
| `Database` | Disabled | Disabled | `Orm` 借用宿主的 FreeSql。事务使用 `InTransactionAsync` 回调的 `unit.Orm` 或绑定该工作单元的仓储。关闭时访问 ORM 或创建工作单元会抛出异常。 |
| `MessageQueue` | Disabled | Disabled | 发布、订阅、`Producer<T>()`、`DeadLetters<T>()`、`RetryPolicy` 和 `Tracer` 位于同一入口。必需消息操作会报告关闭。应用可以显式跳过可选通知。 |
| `JobScheduler` | Disabled | Disabled | 调度、触发与查询作业。共享调度器的准备、启动、关闭和释放由宿主负责。 |
| `IdentityContext` | Enabled | Disabled | 宿主在没有已建立身份时提供 `AsgardIdentitySnapshot.Empty`，`UserInfo` 可为 `null`，租户 ID 是 `Guid.Empty`。未装配能力时读取身份会抛出关闭异常。 |
| `TenantScopeFactory` | Enabled | Disabled | 调用方先取得目标租户授权，再通过 `CreateScope(tenantId)` 建立作用域。`Guid.Empty` 无效。跨租户作用域需要服务端授权器。 |
| `Trace` | Enabled | Disabled | `IsEnabled` 表示当前请求有活动追踪。未采集时 `TraceId` 为空，备注、标签和分支可安全追加。`Trace.Query` 默认 Disabled。 |
| `Log` | Enabled | Disabled | 业务日志默认分类 `Asgard.Business`。`WriteStatus` 表示已装配日志工厂，`IsEnabled(level)` 检查分类和级别。未装配时写日志为空操作。`Log.Query` 默认 Disabled。 |
| `Config` | Enabled | Disabled | `Get<TConfig>()` 在当前作用域读取已注册的 `ISystemConfig` 强类型对象。`Configuration` 是允许展示的配置摘要，`Validate()` 校验配置。 |
| `Encryption` | Disabled | Disabled | 省略 AES 密钥与 IV 时关闭。提供其中一项就要完整校验，Base64 或长度错误会使宿主构建失败。 |
| `PasswordHasher` | Enabled | Disabled | 哈希、校验密码和判断是否需要补算哈希。Yggdrasil 自动注册，独立于 AES 配置。 |
| `KeyGenerator` | Enabled | Disabled | 生成随机密钥、HMAC 密钥及 AES 密钥和 IV。Yggdrasil 自动注册。 |
| `Compression` | Enabled | Disabled | Brotli 压缩、解压。Yggdrasil 自动注册。 |
| `WildcardMatcher` | Enabled | Disabled | 文本通配匹配。Yggdrasil 自动注册。 |

`ctx.GetFeatures()` 返回只读的 `IReadOnlyDictionary<string, IAsgardFeature>`。键是上表 14 个属性名。检查 `Status`、`Configuration`、`LastError` 即可观察模块，无需打开连接。通过 `IAsgardFeature` 类型输出诊断，将授权和可返回的字段限定在运维边界内。

| 兼容属性 | 新代码入口 |
| --- | --- |
| `ctx.Cache` | `ctx.Redis.Cache` |
| `ctx.RedisPubSub` | `ctx.Redis.PubSub` |
| `ctx.RedisStream` | `ctx.Redis.Stream` |
| `ctx.DistributedLock` | `ctx.Redis.Locks` |
| `ctx.DatabaseLogQuery` | `ctx.Log.Query` |
| `ctx.SystemConfig` | `ctx.Config` |

## 独立装配的子能力

- Redis 只装配子服务时根可为 `PartiallyEnabled`。Pipeline、事务和原生数据命令看 `Redis.CommandStatus`，发布、消费或互斥看 `PubSub.Status`、`Stream.Status` 或 `Locks.Status`。
- 当前请求追踪和持久化历史查询独立。`trace.enabled` 控制默认历史存储。开关关闭时 `Trace.Query.Status` 为 `Disabled`，宿主仍可记录活动请求。在只有历史查询的自定义容器中，`Trace.Status` 为 `PartiallyEnabled`。
- 写日志和持久化查询独立。`logging.database.enabled` 控制默认日志查询。仅安装查询服务时 `Log.Status` 可为 `PartiallyEnabled`，`Log.WriteStatus` 为 `Disabled`。两个模块的历史查询都读取宿主级数据，应用需先验证运维权限并裁剪返回字段。
- `MessageQueue.Tracer` 还取决于 `messaging.tracing.enabled` 和实际注册的追踪器。通过 `ctx.MessageQueue.IsHealthyAsync(ct)` 探测实际队列连接。服务的 `Enabled` 状态只说明装配条件满足。
- 运维读取 `Config.Configuration`，业务用 `Config.Get<TConfig>()` 读取自己所需的强类型配置。配置对象可能含有凭据，原始对象仅供应用内消费。单独注册一种业务配置也可通过 `Config` 读取。
- 能力关闭时，要求相应基础设施的调用会抛 `AsgardFeatureDisabledException`。成功调用不会清空上一次失败，主动取消不会覆盖 `LastError`。依赖实时健康由就绪探测提供。见 [宿主健康检查](../../asgard-host-features/SKILL.md)。

## 使用 8.0 的项目

8.0 同样有 14 个根入口。使用 `SystemConfig` 代替表中的 `Config`，使用 `DatabaseLogQuery` 作为独立根入口。最小 Yggdrasil 中 `SystemConfig` 为 Enabled，`DatabaseLogQuery` 为 Disabled。空容器的 14 个入口全部 Disabled。该版本有表中的前四个 Redis 兼容属性。8.1 新增日志写入、强类型配置读取、数据库 ORM、队列辅助操作和 `GetFeatures()`。
