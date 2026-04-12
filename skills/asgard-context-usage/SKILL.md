---
name: asgard-context-usage
description: AsgardContext 使用 skill。Use when a task needs AsgardContext, AbsAsgardContext, shared infrastructure access, null-safe capability usage, Trace note/tag usage, service lifetime reasoning, or guidance on whether to use context versus direct dependency injection in Asgard.
---

# Asgard Context Usage

## 作用

`AbsAsgardContext` 是 Asgard 框架的**公共能力聚合入口**，所有可选基础设施能力（缓存、消息队列、作业调度、加密、轻量追踪等）都通过 Context 统一访问。这种设计避免了循环依赖，支持可选模块优雅降级。

## 什么时候使用

- **需要访问公共基础设施能力时** - 通过 Context 获取缓存、消息队列、加密等服务
- **在业务服务中需要跨模块能力** - 通过 Context 聚合入口避免直接依赖多个模块
- **需要处理可选模块降级** - 未启用的模块返回 null，可以优雅降级
- **需要在后台任务中创建租户作用域** - 通过 `TenantScopeFactory` 创建隔离作用域

## Context 可获取的能力列表

| 属性 | 能力说明 | 模块 |
|------|----------|------|
| `Cache` | 多级缓存（内存 + Redis） | 缓存模块 |
| `Compression` | 数据压缩（Brotli）| 压缩模块 |
| `TenantScopeFactory` | 租户作用域工厂 | 租户模块 |
| `IdentityContext` | 当前身份上下文 | 身份认证模块 |
| `JobScheduler` | 作业调度器 | 作业调度模块 |
| `MessageQueue` | 消息队列 | 消息模块 |
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
| **可空性** | 所有能力都是 `?` 可空，模块未启用时为 `null` |
| **调用方式** | 使用 `?.` 调用，必须做空检查 |
| **降级策略** | 能力为 `null` 时，降级到直接查询/处理 |
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

### 业务服务注入

```csharp
/// <summary>
/// {ServiceSummary}
/// </summary>
public class {ServiceName}
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asgardContext">Asgard 上下文</param>
    public {ServiceName}(AbsAsgardContext asgardContext)
    {
        AsgardContext = asgardContext;
    }

    /// <summary>
    /// Asgard 上下文
    /// </summary>
    protected AbsAsgardContext AsgardContext { get; }
}
```

### 缓存读取（带优雅降级）

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <returns>查询结果</returns>
public async Task<{ResultType}?> Get{ResultName}Async({ParameterType} {ParameterName})
{
    var cacheKey = $"{ModuleName}:{EntityName}:{ParameterName}";
    
    // 先尝试从缓存获取（空检查支持优雅降级）
    if (AsgardContext.Cache != null)
    {
        var cached = await AsgardContext.Cache.GetAsync<{ResultType}>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
    }

    // 缓存未命中或缓存未启用，降级到直接查询
    var result = await _{repositoryName}.GetByIdAsync({ParameterName});
    
    // 写入缓存
    if (result != null && AsgardContext.Cache != null)
    {
        await AsgardContext.Cache.SetAsync(cacheKey, result);
    }

    return result;
}
```

### 后台作业创建租户作用域

```csharp
/// <summary>
/// 后台作业执行
/// </summary>
/// <param name="cancellationToken">取消令牌</param>
public async Task ExecuteAsync(CancellationToken cancellationToken)
{
    // 需要在后台任务中创建租户作用域时，使用 TenantScopeFactory
    if (AsgardContext.TenantScopeFactory != null)
    {
        using var scope = AsgardContext.TenantScopeFactory.CreateScope({TenantId});
        // 在作用域内执行业务逻辑时，FreeSql 全局过滤和 Asgard 仓储会自动读取当前租户
        await {BusinessLogic}(cancellationToken);
    }
    else
    {
        // 租户作用域工厂未注册，降级处理
        await {FallbackLogic}(cancellationToken);
    }
}
```

### 注册服务（Program.cs）

```csharp
// 注册顺序：先注册其他模块，最后注册 Asgard Context
builder.Services.AddMultiLevelCache(builder.Configuration);
builder.Services.AddMessageQueue(builder.Configuration);
builder.Services.AddJobScheduler(builder.Configuration);
builder.Services.AddAsgardContext(); // 最后注入，确保所有服务都已注册
```

### 追加轻量追踪备注

```csharp
/// <summary>
/// 创建订单
/// </summary>
/// <param name="command">订单命令</param>
/// <returns>订单标识</returns>
public async Task<Guid> CreateOrderAsync(CreateOrderCommand command)
{
    AsgardContext.Trace?.AddTag("OrderId", command.OrderId.ToString());
    AsgardContext.Trace?.AddBranch("OrderCreate", "ValidateBeforePersist");
    AsgardContext.Trace?.AddNote("该备注用于反推单元测试输入，不用于记录完整对象图。");

    return await _orderRepository.InsertAsync(command.ToEntity());
}
```

## 推荐做法

- 把 `AbsAsgardContext` 当作公共能力的统一入口，简化依赖注入
- 访问任何能力**先判空**，支持模块动态启用禁用
- 判空后**一定要降级**，不要因为模块未启用就直接抛出异常
- 需要后台租户作用域时，优先使用 `TenantScopeFactory`
- 需要后台租户数据库访问时，先进入 `TenantScopeFactory.CreateScope(tenantId)`，再调用仓储或 `IFreeSql`
- 在其他模块都注册完成后，再调用 `AddAsgardContext()`
- 需要定位运行链路或补充测试线索时，优先用 `AsgardContext.Trace` 追加简明备注和标签

## 不要这样做

❌ 不要假设 `Cache`、`MessageQueue`、`JobScheduler` 一定存在，始终做空检查

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
