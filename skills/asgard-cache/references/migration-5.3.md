# Asgard 5.3.0 缓存迁移（6.0 删除旧声明）

5.3 起业务缓存仅使用 Redis，不再有本地层、回填、两秒 TTL 或内存故障回退。整个 5.x 保留旧声明并使用 `[Obsolete(..., error: true)]` 提供编译错误；6.0 删除这些声明。这是强制源码迁移，不是无感兼容升级，也不保证旧插件二进制直接加载。请同时重新编译宿主和插件。

## C# 迁移

| 旧入口 | 替换方式 |
| --- | --- |
| `IMultiLevelCache` | `IAsgardCache` |
| `AddMultiLevelCache(...)` | `AddAsgardCache(...)`；Yggdrasil 默认链路已自动装配 |
| `MultiLevelCache` | 默认从 DI 获取 `IAsgardCache`；手动装配使用 `RedisAsgardCache` |
| `MultiLevelCacheConfigurator` | `RedisCacheConfigurator` |
| `CacheConfig.Memory` / `MemoryCacheOptions` | 删除；业务缓存不再支持内存模式 |
| `RedisCacheOptions.Enabled` | 使用 `CacheConfig.Enabled` |
| `RedisCacheOptions.FallbackToMemoryCache` | 删除；故障会传播异常 |
| `CacheManager.MemoryCache` | 删除调用；认证内部内存缓存独立使用 `AddMemoryCache()` |
| `RefreshAsync<T>(key)` | 使用 `GetAsync<T>(key)`，直接读取 Redis |
| `ClearAsync()` | 使用 `RemoveByPrefixAsync("业务前缀:")`；普通接口不再提供整库清空 |

仓储构造参数改为 `IAsgardCache cache` 后继续传给基类。`AbsAsgardContext.Cache` 属性名不变，类型变为 `IAsgardCache?`。仓储自动缓存和租户键规则本次保留。

```csharp
// 非 Yggdrasil 宿主的独立注册入口。
services.AddAsgardCache(new CacheConfig
{
    Enabled = true,
    Redis = new RedisCacheOptions
    {
        ConnectionString = "localhost:6379",
        InstanceName = "MyApp:",
        DefaultExpirationMinutes = 30
    }
});
```

不要通过抑制错误继续使用旧入口。应完成提示中的替换；继承旧接口的测试替身或第三方缓存实现也需要迁移，并实现前缀删除契约。

## YAML 迁移

```yaml
caching:
  enabled: true
  redis:
    connectionString: "localhost:6379"
    instanceName: "MyApp:"
    database: 0
    defaultExpirationMinutes: 30
```

必须删除整个 `caching.memory` 节点、`caching.redis.enabled`、`caching.redis.fallbackToMemoryCache`。不能仅改成 `false`：配置加载器在绑定之前检查旧键，空节点、null 和 false 同样会触发迁移错误。合并配置中的环境变量、命令行旧键也需要清理。

原先只使用内存缓存的部署必须选择配置 Redis，或明确设置 `caching.enabled: false`。框架不会静默使用内存、忽略旧配置或替用户选择 Redis 部署。

## 运行行为

- 缓存默认关闭。关闭时提供可注入的空缓存，读取未命中，写入与删除无操作，不创建 Redis 连接。
- 启用时启动验证 Redis；故障不自动退回本地。运行时读写和失效失败传播异常，不能把删除失败当作成功。
- 显式 TTL 优先，否则使用 `caching.redis.defaultExpirationMinutes`。所有实例以 Redis 过期为准。
- Redis 序列化格式和 `InstanceName` 前缀保持原方式，读取返回独立反序列化对象。移除本地层会增加 Redis 请求量，应结合部署负载验证。
- 前缀删除只处理应用命名空间内的字面前缀，Redis glob 字符会转义。扫描删除不是并发写入事务，也不提供数据库与缓存的强一致性。
- Yggdrasil 保留 Redis 连接复用、分布式锁自动注册和 DataProtection 密钥共享。业务本地缓存不再注册全局 `IMemoryCache`，OIDC/JWKS 仍独立缓存。
- 本次不修改仓储默认缓存策略，不解决数据库更新与缓存回填之间所有并发竞争。

## 发布与清理清单

5.3.0 发布强制迁移入口和本文档。整个 5.x 继续保留带 error:true 的旧接口、类型、注册方法、配置属性与成员。6.0 删除旧声明和旧入口，更新迁移测试；新代码自 5.3 起不得使用这些入口。

团队 AI 使用的 skill 位于 仓库 doc/skills/asgard-cache/SKILL.md，发布升级时同步分发整个 skill 目录。
