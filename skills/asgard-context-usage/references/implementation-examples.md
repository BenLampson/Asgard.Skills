## 代码示例

### 按模块查看运行诊断

运维入口先校验访问权限，再遍历 `GetFeatures()`。每项都有稳定的名字和只读诊断契约。

```csharp
var statuses = ctx.GetFeatures().ToDictionary(entry => entry.Key, entry => entry.Value.Status);
var redisFailure = ctx.Redis.LastError;
ctx.Log.LogInformation("作业模块状态为 {JobStatus}", statuses[nameof(ctx.JobScheduler)]);
```

应用内可通过 `ctx.Config.Get<OrdersConfig>()` 获取已在 DI 注册的配置。返回给运维调用方的配置字段取自 `ctx.Config.Configuration`。租户数据访问继续使用授权后的作用域与仓储。

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
public async Task<{ResultType}?> {MethodName}({ParameterType} {ParameterName})
{
    var cacheKey = string.Concat("{ModuleName}:{EntityName}:", {ParameterName});

    var cache = AsgardContext.Redis.Cache;
    var cached = await cache.GetAsync<{ResultType}>(cacheKey);
    if (cached != null) return cached;

    var result = await _{repositoryName}.{QueryMethod}({ParameterName});
    if (result != null) await cache.SetAsync(cacheKey, result);
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
    if (AsgardContext.TenantScopeFactory.Status == AsgardFeatureStatus.Enabled)
    {
        using var scope = AsgardContext.TenantScopeFactory.CreateScope({TenantId});
        // 在作用域内执行业务逻辑时，FreeSql 全局过滤和 Asgard 仓储会自动读取当前租户
        await {BusinessLogic}(cancellationToken);
    }
    else
    {
        // 租户隔离是本任务前提，缺少工厂时终止，不能脱离租户执行。
        throw new InvalidOperationException("租户作用域工厂未注册。");
    }
}
```

### 分布式锁使用（互斥前提不可跳过）

```csharp
/// <summary>
/// 执行单实例任务
/// </summary>
/// <param name="cancellationToken">取消令牌</param>
public async Task ExecuteOnceAsync(CancellationToken cancellationToken)
{
    if (AsgardContext.Redis.Locks.Status == AsgardFeatureStatus.Disabled)
    {
        throw new InvalidOperationException("分布式锁未装配，不能执行需要互斥的任务。");
    }

    await using var handle = await AsgardContext.Redis.Locks.TryAcquireAsync(
        "jobs:{JobName}",
        new DistributedLockAcquireOptions
        {
            LeaseTime = TimeSpan.FromMinutes(2)
        },
        cancellationToken);

    if (handle == null)
    {
        return;
    }

    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken,
        handle.LockLostToken);

    await {BusinessLogic}(operationCancellation.Token);
}
```

分布式锁的自动装配、默认参数、自动续租和 `LockLostToken` 语义统一转到 `$asgard-distributed-lock`，不要在 Context skill 中重复定义锁契约。

### 注册服务（Program.cs）

```csharp
// 自定义宿主使用已合并的 AsgardConfigurationRoot configuration。Yggdrasil 会自动完成这些注册。
_ = builder.Services.AddAsgardCache(configuration.Load<CacheConfig>());
_ = builder.Services.AddMessageQueue(configuration.Load<MQConfig>());
_ = builder.Services.AddJobScheduler(configuration.Load<JobConfig>());
_ = builder.Services.AddAsgardContext();
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
    AsgardContext.Trace.AddTag("OrderId", command.OrderId.ToString());
    AsgardContext.Trace.AddBranch("OrderCreate", "ValidateBeforePersist");
    AsgardContext.Trace.AddNote("该备注用于反推单元测试输入，不用于记录完整对象图。");

    return await _orderRepository.InsertAsync(command.ToEntity());
}
```
