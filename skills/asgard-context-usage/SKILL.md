---
name: asgard-context-usage
description: "通过 AbsAsgardContext 使用 Asgard 8 的非空能力入口、模块状态、脱敏配置、最近异常、租户作用域和历史 Trace 查询。各模块操作见对应 skill。"
---

# Asgard Context

在业务服务构造函数注入 `AbsAsgardContext`。控制器使用 `BaseController.AsgardContext`，插件及后台任务在各自 DI 作用域中解析。Context 的生命周期是 Scoped。需要独立处理多项工作的单例应为每次处理创建作用域。

## 能力入口

| 入口 | 用途 |
| --- | --- |
| `ctx.Redis` | `Cache`、`PubSub`、`Stream`、`Locks`、Pipeline、条件事务、Lua、数据结构及原生 `Data` / `Streams`。见 [Redis](../asgard-cache/SKILL.md) |
| `ctx.Database` | 在业务数据库上创建工作单元，或通过 `InTransactionAsync` 执行、提交与回滚事务 |
| `ctx.JobScheduler`、`ctx.MessageQueue` | 业务调度、发布和订阅。单个作用域可以管理自己创建的订阅，宿主负责共享服务的启停与释放 |
| `ctx.IdentityContext`、`ctx.TenantScopeFactory` | 当前身份及授权后的显式租户作用域。具体字段见 [身份](../asgard-identity-userinfo/SKILL.md) |
| `ctx.Trace`、`ctx.Trace.Query` | 为当前请求追加标签、分支、备注。历史查询可按时间、TraceId 检索持久化记录 |
| `ctx.DatabaseLogQuery` | 查询宿主级持久化日志。应用先做运维授权，再裁剪可返回的字段 |
| `ctx.SystemConfig` | `Validate()` 校验配置，`Configuration` 展示白名单中的宿主有效配置 |
| `ctx.Encryption`、`ctx.PasswordHasher`、`ctx.KeyGenerator` | 加解密、密码哈希和密钥生成 |
| `ctx.Compression`、`ctx.WildcardMatcher` | 压缩和通配符匹配 |

## 状态与错误

`ctx.Redis` 的静态类型为 `IAsgardRedis`。每个 Context 能力都实现 `IAsgardFeature`，未配置时也可读取 `Status`、`Configuration`、`LastError`。`Disabled` 表示关闭；自定义容器只装配部分 Redis 能力时，根对象可为 `PartiallyEnabled`。检查相应子模块，命令与 Pipeline 查看 `ctx.Redis.CommandStatus`。观察模块状态不会连接服务。就绪探测配置见 [宿主功能](../asgard-host-features/SKILL.md)。

`Configuration` 只供展示，`LastError` 是当前宿主内最近一次失败的脱敏摘要，含操作名、异常类型、时间和可用的 TraceId。取消保持原有故障记录。通过应用作用域执行的两次调用可以观察到同一模块的故障。

对象缓存关闭时读取未命中，`GetOrCreateAsync` 直接运行回源函数。当前请求未采集追踪时，追加备注可直接调用。需要 Redis、加密、消息或租户隔离的操作在模块关闭时抛 `AsgardFeatureDisabledException`。业务要求互斥或隔离时，应让失败传播。

```csharp
var item = await ctx.Redis.Cache.GetOrCreateAsync(key, ct => repository.LoadAsync(id, ct),
    cancellationToken: cancellationToken);
ctx.Trace.AddNote("已完成订单校验");
```

后台访问指定租户时先验证权限，再调用 `ctx.TenantScopeFactory.CreateScope(tenantId)`，并在所得作用域内执行仓储操作。跨租户查询需要服务端授权。见 [范围与缓存契约](../asgard-database/references/shared-entity-cache-tenant-scopes.md)。

Yggdrasil 在服务注册钩子执行完毕后固定声明。自定义容器可先注册模块，再调用 `AddAsgardContext()`；需要在 Context 注册之后声明惰性能力时，用 `AddAsgardFeature<T>` 指定 `status` 和可展示的 `configuration`。普通工厂在首次业务操作时解析并补充自身状态。若要反复修改同一服务集合、构建多个相互独立的容器，使用 `services.BuildAsgardServiceProvider()`，每个容器会固定自己最终的声明。

已有 `ctx.Cache`、`RedisPubSub`、`RedisStream`、`DistributedLock` 为兼容别名，新代码统一从 `ctx.Redis` 调用。接口契约见 [IAsgardFeature](references/IAsgardFeature.cs)。更多示例见 [业务调用](references/implementation-examples.md)。
