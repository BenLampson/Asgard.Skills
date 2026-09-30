---
name: asgard-context-usage
description: "在 Asgard 中使用 AbsAsgardContext，处理公共能力获取、可空性、生命周期、租户作用域和 Trace 备注。缓存与锁的详细契约使用对应模块 skill。"
---

# Asgard Context Usage

## 缓存版本边界

缓存示例面向 Asgard 5.3+，使用 `IAsgardCache`。默认 Yggdrasil 宿主自动装配缓存；关闭时提供 `NullAsgardCache`，自定义宿主仍需判空。升级与配置迁移读取 `$asgard-cache`。本目录中含旧缓存接口的源码拷贝是 5.3 前快照，只用于维护旧版；不能据此生成 5.3+ 缓存接线。


## 作用

`AbsAsgardContext` 是 Asgard 框架的**公共能力聚合入口**，所有可选基础设施能力（缓存、消息队列、分布式锁、作业调度、加密、轻量追踪等）都通过 Context 统一访问。这种设计避免了循环依赖，支持可选模块优雅降级。

## 什么时候使用

- **需要访问公共基础设施能力时** - 通过 Context 获取缓存、消息队列、加密等服务
- **在业务服务中需要跨模块能力** - 通过 Context 聚合入口避免直接依赖多个模块
- **需要处理可选模块降级** - 未装配的能力可能返回 null；默认宿主关闭缓存时提供 NullAsgardCache
- **需要在后台任务中创建租户作用域** - 通过 `TenantScopeFactory` 创建隔离作用域

## Context 可获取的能力列表

| 属性 | 能力说明 | 模块 |
|------|----------|------|
| `Cache` | Redis 单层业务缓存（5.3+） | 缓存模块 |
| `Compression` | 数据压缩（Brotli）| 压缩模块 |
| `TenantScopeFactory` | 租户作用域工厂 | 租户模块 |
| `IdentityContext` | 当前身份上下文 | 身份认证模块 |
| `JobScheduler` | 作业调度器 | 作业调度模块 |
| `MessageQueue` | 消息队列 | 消息模块 |
| `DistributedLock` | 基于 Redis 的分布式锁 | 分布式锁模块 |
| `Encryption` | 加密服务（AES、MD5）| 加密模块 |
| `PasswordHasher` | 密码哈希（BCrypt）| 安全模块 |
| `KeyGenerator` | 密钥生成 | 加密模块 |
| `SystemConfig` | 系统配置 | 配置模块 |
| `WildcardMatcher` | 通配符匹配 | 工具模块 |
| `Trace` | 当前请求轻量追踪上下文 | 可观测性 / 追踪模块 |

## 获取方式

| 获取场景 | 方式 |
|----------|------|
| **控制器中** | 继承 `BaseController` 后直接使用 `AsgardContext` 字段 |
| **业务服务中** | 构造函数注入 `AbsAsgardContext` |
| **插件中** | `InitializeAsync` 之后通过 `GetAsgardContext()` 获取 |

## 核心规则

| 规则 | 说明 |
|------|------|
| **生命周期** | `AbsAsgardContext` 是 **Scoped** 生命周期，每次请求创建新实例 |
| **可空性** | 能力声明可空；自定义宿主未装配时可能为 `null`，默认宿主关闭缓存时提供 `NullAsgardCache` |
| **调用方式** | 使用 `?.` 调用，必须做空检查 |
| **降级策略** | 缓存未装配或未命中时可回源；身份、租户隔离与分布式互斥不能静默跳过 |
| **注册顺序** | 先注册其他模块，**最后**调用 `AddAsgardContext()` |
| **身份模型** | `IdentityContext.UserInfo` 的统一模型是 `AbsAsgardUserInfo`，需要字段语义与 claim 契约时转到 `$asgard-identity-userinfo` |
| **租户注入** | `TenantScopeFactory` 创建的作用域会把租户写入身份上下文，随后 FreeSql 仓储和全局过滤会自动读取 |
| **追踪补充** | `Trace` 只允许追加备注、标签和分支说明，不暴露框架步骤的修改入口 |

## `IdentityContext` 特别说明

- `IdentityContext` 负责暴露当前请求的身份快照，而不是让业务层自己到处解析 `ClaimsPrincipal`
- `IdentityContext.UserInfo` 的标准模型是 `AbsAsgardUserInfo`
- 如果你需要定义 IDP 输出、用户字段扩展、claim 命名、测试登录态，请不要在本 skill 里自行发挥，直接切到 `$asgard-identity-userinfo`

## `Trace` 特别说明

- `Trace` 用于给当前 HTTP 请求补充**可用于定位问题和反推测试条件**的信息
- 它不是全量审计日志，也不是给你转储任意对象图的入口
- 业务代码可以调用：
  - `AsgardContext.Trace?.AddNote(...)`
  - `AsgardContext.Trace?.AddTag(...)`
  - `AsgardContext.Trace?.AddBranch(...)`
- 如果问题本身是“框架追踪能力怎么设计、为什么只记录轻量摘要、哪些入口会自动记步骤”，直接切到 `$asgard-tracing-observability`

## 代码示例

需要编写该模块代码时，按场景读取 [实现示例](references/implementation-examples.md)，只采用与当前任务和目标版本匹配的示例。

## 推荐做法

- 把 `AbsAsgardContext` 当作公共能力的统一入口，简化依赖注入
- 访问任何能力**先判空**，支持模块动态启用禁用
- 判空后**一定要降级**，不要因为模块未启用就直接抛出异常
- 需要多实例互斥时，优先通过 `AsgardContext.DistributedLock` 获取锁能力
- 长时间持锁时，把 `handle.LockLostToken` 与业务取消令牌合并，锁所有权丢失后立即停止受保护操作
- 需要后台租户作用域时，优先使用 `TenantScopeFactory`
- 需要后台租户数据库访问时，先进入 `TenantScopeFactory.CreateScope(tenantId)`，再调用仓储或 `IFreeSql`
- 在其他模块都注册完成后，再调用 `AddAsgardContext()`
- 需要定位运行链路或补充测试线索时，优先用 `AsgardContext.Trace` 追加简明备注和标签

## 不要这样做

❌ 不要假设 `Cache`、`MessageQueue`、`DistributedLock`、`JobScheduler` 一定存在，始终做空检查

❌ 不要把所有依赖都替换成 `IServiceProvider`，`AbsAsgardContext` 已经提供了更稳定的类型入口

❌ 不要在单例服务中长期持有 scoped 的 `AbsAsgardContext`，会造成生命周期问题

❌ 不要先注册 `AddAsgardContext()` 再注册其他模块，这样无法注入已注册的服务

❌ 不要跳过空检查直接使用 `!` 强制非空，模块未启用时会抛出空引用异常

❌ 不要在后台任务里手动拼接默认租户过滤，如果已经进入 `TenantScopeFactory` 作用域，框架会自动把租户传给 FreeSql

❌ 不要把 `AsgardContext.Trace` 当作大对象序列化出口，它应该只承载轻量说明信息

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `AbsAsgardContext.cs` - 上下文抽象类，定义所有能力属性
- `AsgardContext.cs` - 具体实现类
- `AsgardContextServiceCollectionExtensions.cs` - DI 注册扩展

代码范本请参考 `templates/` 目录，可直接替换占位符使用。
