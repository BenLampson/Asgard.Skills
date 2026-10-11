---
name: asgard-cache
description: Configure and use Asgard 6.0+ Redis business caching with IAsgardCache, TTL, invalidation and disabled-cache behavior. Excludes OIDC/JWKS internal memory caching.
---

# Asgard Redis 缓存

## 当前版本

适用于 Asgard 6.0+，业务缓存仅支持 Redis。通过 `IAsgardCache` 使用，默认宿主自动装配；自定义宿主使用 `AddAsgardCache`，手动装配使用 `RedisAsgardCache` / `RedisCacheConfigurator`。

所有示例和实现只使用当前 API。业务缓存没有本地层、两级回填或内存回退。

## YAML

```yaml
caching:
  enabled: true
  redis:
    connectionString: "localhost:6379"
    instanceName: "MyApp:"
    database: 0
    defaultExpirationMinutes: 30
```

必须删除整个 `caching.memory` 节点、`caching.redis.enabled` 和 `caching.redis.fallbackToMemoryCache`。false、null、空节点仍是旧键，会在绑定前报迁移错误。环境变量、命令行、合并配置文件中的同名旧键也必须清理。

原来只用内存缓存的部署，应由项目明确配置 Redis 或关闭 `caching.enabled`；不能静默选择 Redis 地址或自动关闭缓存。

## 使用与行为

- 服务优先从 `AbsAsgardContext.Cache` 获取；保留空检查，支持自定义宿主未装配的情况。
- 仓储基类构造参数使用 `IAsgardCache`。当前版本保留自动 CRUD 缓存、租户键规则；不要擅自取消这些行为。
- 缓存关闭时默认宿主提供 `NullAsgardCache`：不创建连接，读取未命中、写入删除无操作。
- 启用时校验 Redis 连接，运行时故障传播异常。不把删除失败当成功，不自动回退内存。
- 显式 TTL 必须大于零；未指定时使用 `caching.redis.defaultExpirationMinutes`。TTL 只由 Redis 决定。
- 缓存读取为独立反序列化快照，不共享可变实体引用。未命中由业务回源；缓存不是唯一数据源。
- 前缀删除限定应用 InstanceName，前缀按字面匹配。扫描删除不保证与并发写入原子一致，Redis 单层也不消除数据库与缓存回填的竞争。
- Yggdrasil 自动提供分布式锁并与 DataProtection 复用 Redis 连接。不要为迁移缓存另造连接或改变锁语义。
- OIDC/JWKS 的内部 IMemoryCache 独立保留，不属于业务缓存，不应一并删除。

## 参考与分发

- `references/CacheConfig.cs`、`references/CacheManager.cs`：当前配置与生命周期实现。
- `templates/appsettings.yaml.template`：新配置模板。
- 仓库 `doc/skills/asgard-cache` 提供同版 skill，团队升级时同步分发整个目录。
