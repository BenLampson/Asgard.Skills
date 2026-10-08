---
name: asgard-cache
description: "配置、使用或迁移 Asgard 5.3+ Redis 业务缓存 IAsgardCache，处理 TTL、失效和关闭行为。包含旧缓存 API 的迁移；不涉及 OIDC/JWKS 内部内存缓存。"
---

# Asgard Redis 缓存

## 版本与强制迁移

Asgard 5.3 起业务缓存仅支持 Redis；无本地层、两级回填、固定两秒 TTL 或内存回退。旧声明整个 5.x 保留并标记 `[Obsolete(..., error: true)]`，6.0 删除。不要生成旧 API 或建议压制错误；按提示迁移接口、配置并重新编译宿主和插件，不承诺二进制兼容。

- `IMultiLevelCache` → `IAsgardCache`。
- `AddMultiLevelCache(...)` → `AddAsgardCache(...)`。Yggdrasil 已自动装配，不重复注册。
- `MultiLevelCache` → 默认从 DI 获取 `IAsgardCache`；手动装配用 `RedisAsgardCache`。
- `MultiLevelCacheConfigurator` → `RedisCacheConfigurator`。
- `RefreshAsync<T>` → `GetAsync<T>`，已无本地副本可刷新。
- `ClearAsync` → 选择业务前缀调用 `RemoveByPrefixAsync`；不引导业务执行 FLUSHDB。
- 删除 `MemoryCacheOptions` / `CacheConfig.Memory` / `CacheManager.MemoryCache` 调用。
- `RedisCacheOptions.Enabled` → `CacheConfig.Enabled`；删除 `FallbackToMemoryCache`。

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
- 仓储基类构造参数使用 `IAsgardCache`。单实体共享缓存补丁将实体键改为数据身份键，平台与所属租户共享同一条缓存；列表键/失效规则未改。先核对目标提交，按下方迁移说明处理，不沿用旧实体租户分段键。
- 缓存关闭时默认宿主提供 `NullAsgardCache`：不创建连接，读取未命中、写入删除无操作。
- 启用时校验 Redis 连接，运行时故障传播异常。不把删除失败当成功，不自动回退内存。
- 显式 TTL 必须大于零；未指定时使用 `caching.redis.defaultExpirationMinutes`。TTL 只由 Redis 决定。
- 缓存读取为独立反序列化快照，不共享可变实体引用。未命中由业务回源；缓存不是唯一数据源。
- 前缀删除限定应用 InstanceName，前缀按字面匹配。扫描删除不保证与并发写入原子一致，Redis 单层也不消除数据库与缓存回填的竞争。
- Yggdrasil 自动提供分布式锁并与 DataProtection 复用 Redis 连接。不要为迁移缓存另造连接或改变锁语义；宿主生命周期补丁下，DI 获得的共享连接/缓存为借用实例，不由业务或插件手动释放。宿主统一资源所有权见 `$asgard-host-project`。
- OIDC/JWKS 的内部 IMemoryCache 独立保留，不属于业务缓存，不应一并删除。

## 单实体共享缓存补丁

本补丁以 Asgard 提交 `0abb1d959418c4b877ef3fc909abdee553e7b11f` 为核对基线；不要据此假定所有 5.3 包都已有此行为，也不要自动升级依赖。涉及仓储缓存、平台/租户访问、工作单元或部署迁移时，读取 [共享缓存与显式租户范围](../asgard-database/references/shared-entity-cache-tenant-scopes.md)。

- 不新增配置。复用现有 Redis 逻辑数据库与 `InstanceName`；一个缓存范围对应一个逻辑业务数据库/schema
- 键不含调用者租户；缓存命中仍须检查真实实体归属，不能把共享缓存当跨租户授权
- 空租户默认拒绝；跨租户访问必须由服务端授权器通过 `CreateCrossTenantScope()` 建立
- 工作单元内不读写共享实体缓存，提交后统一失效；数据库提交后的失效异常不意味着回滚
- cache-aside 不保证线性一致；本补丁不提供 outbox，也不修复列表缓存算法

## 参考与分发

- [迁移说明](references/migration-5.3.md)：发布期和 6.0 清理清单。
- `references/CacheConfig.cs`、`references/CacheManager.cs`：当前配置与生命周期实现。
- `templates/appsettings.yaml.template`：新配置模板。
- 仓库 `doc/skills/asgard-cache` 提供同版 skill，团队升级时同步分发整个目录。
