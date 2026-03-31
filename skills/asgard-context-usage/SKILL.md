---
name: asgard-context-usage
description: AsgardContext 使用 skill。Use when a task needs AsgardContext, AbsAsgardContext, shared infrastructure access, null-safe capability usage, service lifetime reasoning, or guidance on whether to use context versus direct dependency injection in Asgard.
---

# Asgard Context Usage

## 作用

`AbsAsgardContext` 是 Asgard 框架的**公共能力聚合入口**，所有可选基础设施能力（缓存、消息队列、作业调度、加密等）都通过 Context 统一访问。这种设计避免了循环依赖，支持可选模块优雅降级。

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
        await using var scope = await AsgardContext.TenantScopeFactory.CreateScopeAsync({TenantId}, cancellationToken);
        // 在作用域内执行业务逻辑，可以正确获取租户上下文
        await {BusinessLogic}(scope.ServiceProvider, cancellationToken);
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

## 推荐做法

- 把 `AbsAsgardContext` 当作公共能力的统一入口，简化依赖注入
- 访问任何能力**先判空**，支持模块动态启用禁用
- 判空后**一定要降级**，不要因为模块未启用就直接抛出异常
- 需要后台租户作用域时，优先使用 `TenantScopeFactory`
- 在其他模块都注册完成后，再调用 `AddAsgardContext()`

## 不要这样做

❌ 不要假设 `Cache`、`MessageQueue`、`JobScheduler` 一定存在，始终做空检查

❌ 不要把所有依赖都替换成 `IServiceProvider`，`AbsAsgardContext` 已经提供了更稳定的类型入口

❌ 不要在单例服务中长期持有 scoped 的 `AbsAsgardContext`，会造成生命周期问题

❌ 不要先注册 `AddAsgardContext()` 再注册其他模块，这样无法注入已注册的服务

❌ 不要跳过空检查直接使用 `!` 强制非空，模块未启用时会抛出空引用异常

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `AbsAsgardContext.cs` - 上下文抽象类，定义所有能力属性
- `AsgardContext.cs` - 具体实现类
- `AsgardContextServiceCollectionExtensions.cs` - DI 注册扩展

代码范本请参考 `templates/` 目录，可直接替换占位符使用。
