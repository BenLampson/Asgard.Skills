---
name: asgard-cache
description: Asgard 缓存模块 skill。Use when configuring or using Asgard caching, including CacheConfig, memory cache, Redis cache, multi-level cache behavior, cache access through AbsAsgardContext, cache keys, expiration, and cache-related graceful degradation.
---

# Asgard Cache

## 作用

用于配置和使用 Asgard 多级缓存系统。Asgard 支持内存缓存（一级）+ Redis 分布式缓存（二级）的多级缓存策略，获取时先查内存缓存，未命中再查 Redis；写入时同时写入二级缓存。

## 什么时候使用

- **需要配置缓存模块时** - 在项目根目录 `app.yaml` 中配置缓存开关和选项
- **需要在业务代码中使用缓存时** - 通过 `AbsAsgardContext.Cache` 访问缓存服务
- **需要实现缓存穿透保护时** - 缓存未命中自动降级到数据源查询
- **需要维护缓存一致性时** - 更新数据后及时移除失效缓存

## 配置约定

| 配置项 | 说明 | 必填 |
|--------|------|------|
| `caching.enabled` | 是否启用整个缓存模块 | 是 |
| `caching.memory.enabled` | 是否启用内存缓存（一级） | 至少启用一种 |
| `caching.memory.defaultExpirationMinutes` | 内存缓存默认过期分钟数 | 是（启用时） |
| `caching.memory.sizeLimit` | 内存缓存大小限制（字节） | 否 |
| `caching.memory.compactOnMemoryPressure` | 内存压力压缩阈值 (0-1) | 否，默认 0.9 |
| `caching.redis.enabled` | 是否启用 Redis 缓存（二级） | 至少启用一种 |
| `caching.redis.connectionString` | Redis 连接字符串 | 是（启用时） |
| `caching.redis.instanceName` | Redis 实例名称前缀 | 否 |
| `caching.redis.defaultExpirationMinutes` | Redis 默认过期分钟数 | 是（启用时） |
| `caching.redis.database` | Redis 数据库编号 (0-15) | 否 |

**多级缓存策略**：
- 获取：先查内存 → 未命中查 Redis → 都未命中查数据源
- 写入：同时写入内存和 Redis
- 删除：同时从内存和 Redis 删除

## 常用方法

| 方法 | 说明 |
|------|------|
| `GetAsync<T>(key)` | 获取缓存数据 |
| `SetAsync<T>(key, value, expiration)` | 设置缓存数据 |
| `RemoveAsync(key)` | 移除缓存数据 |
| `ExistsAsync(key)` | 检查缓存是否存在 |

## 代码示例

### 配置文件（app.yaml）

```yaml
caching:
  enabled: true
  memory:
    enabled: true
    defaultExpirationMinutes: {DefaultExpirationMinutes}
    sizeLimit: {SizeLimit}
    compactOnMemoryPressure: 0.9
  redis:
    enabled: {EnableRedis}
    connectionString: "{ConnectionString}"
    instanceName: "{InstanceName}"
    defaultExpirationMinutes: {DefaultExpirationMinutesRedis}
    database: {Database}
    connectTimeout: 5000
    syncTimeout: 5000
    asyncTimeout: 5000
    retryCount: 3
    retryIntervalMilliseconds: 1000
```

### 缓存读取（带降级）

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <returns>查询结果</returns>
public async Task<{ResultType}?> {MethodName}({ParameterType} {ParameterName})
{
    var cacheKey = $"{ModuleName}:{EntityName}:{ParameterName}";
    
    // 先尝试从缓存获取
    if (AsgardContext.Cache != null)
    {
        var cached = await AsgardContext.Cache.GetAsync<{ResultType}>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
    }

    // 缓存未命中或缓存未启用，降级到直接查询
    var result = await _{repositoryName}.{QueryMethod}({ParameterName});
    
    // 写入缓存
    if (result != null && AsgardContext.Cache != null)
    {
        await AsgardContext.Cache.SetAsync(cacheKey, result);
    }

    return result;
}
```

### 更新数据后失效缓存

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <returns>操作是否成功</returns>
public async Task<bool> {MethodName}({ParameterType} {ParameterName})
{
    var result = await _{repositoryName}.{UpdateMethod}({ParameterName});
    
    // 更新后失效缓存
    if (result && AsgardContext.Cache != null)
    {
        var cacheKey = $"{ModuleName}:{EntityName}:{ParameterName}";
        await AsgardContext.Cache.RemoveAsync(cacheKey);
    }

    return result;
}
```

## 推荐做法

- 业务服务通过 `AbsAsgardContext.Cache` 获取缓存服务，不要直接注入 `IMultiLevelCache`
- 仓储继承 `AbsAsgardRepositoryBase<TEntity, TKey>` 时必须按基类构造函数注入 `IMultiLevelCache cache`
- 始终对 `AsgardContext.Cache` 做空检查，支持缓存开关动态关闭
- 缓存未命中时一定要降级到数据源查询，不要直接返回 null
- 键名使用 `模块:实体:标识` 格式，保持稳定、可读、可追踪
- 更新/删除数据后，及时移除相关缓存保持一致性
- 内存缓存用于热点数据，Redis 用于分布式共享
- 即使缓存模块关闭，Yggdrasil 也会为仓储构造函数提供可注入的空 `IMultiLevelCache`

## 不要这样做

❌ 不要假设启用了缓存模块就一定启用了 Redis，始终做空检查

❌ 不要忽略 `Cache` 可能为 `null` 的情况，缓存模块可以动态关闭

❌ 不要使用模糊、不可维护的随机键名规则

❌ 不要把缓存当成唯一数据源，缓存只是加速，数据源才是真理之源

❌ 不要更新数据后不清理缓存，导致脏读

❌ 不要同时禁用内存和 Redis，至少启用一种

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `CacheConfig.cs` - 缓存配置类
- `CacheManager.cs` - 缓存管理器

代码范本请参考 `templates/` 目录，可直接替换占位符使用。
