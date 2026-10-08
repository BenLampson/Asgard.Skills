# 单实体共享缓存与显式租户范围

核对基线：[Asgard 0abb1d9](https://github.com/BenLampson/Asgard/commit/0abb1d959418c4b877ef3fc909abdee553e7b11f)。这是行为破坏性补丁，不等于所有已发布 5.3 包；先检查目标源码/依赖是否包含接口和实现，再采用示例，不自动升级项目。

## 数据身份与缓存

- 标准实体的 `Id` 是表内唯一单列主键，不是 `(TenantId, Id)` 联合主键
- 单实体键为 `asgard:entity:v2:{程序集、完整类型和固定表名摘要}:{主键摘要}`。不要手拼或继续覆盖成按当前租户/platform 分段的键
- 平台与所属租户读同一行共享缓存，但缓存命中与数据库结果都要核验真实实体 `TenantId`；其他租户读该实体返回 null，不能删除其合法缓存
- `Get/GetAsync`、`Find/FindAsync` 共用该路径；缓存对象必须是独立反序列化快照，命中后附加到当前仓储跟踪上下文
- 不新增配置，不解析连接串或注册数据源标签；复用已有 Redis database 与 InstanceName。一个缓存范围只能对应一个逻辑业务数据库/schema，所有副本必须一致
- 联合主键、租户局部 ID、动态分库/分表不能套用本缓存。缓存启用时动态 `AsTable/AsType` 等映射被拒绝；应统一关闭该数据源缓存，或另行实现真实数据身份缓存
- 字符串主键按数据库实际返回值归一化，写入可能增加一次无跟踪查库；不要自行用请求字符串造别名缓存
- 列表缓存键和失效算法未改；旧列表键中的 platform 仅是名字，不提供权限

## 授权范围

`AsgardIdentitySnapshot.TenantAccess` 的三种状态：非空租户为 `Tenant`，通过服务端授权器建立的 `CrossTenant`，以及默认拒绝租户数据的 `Unset`。

`Guid.Empty`、`UserType.Platform`、请求参数、客户端 claim 都不能建立跨租户权限。`CreateScope` 是执行范围切换，不替业务验证目标租户授权；应先完成认证和资源授权，再切换到获准租户。

```csharp
using Asgard.Abstractions.Data;

// scopeFactory 由 DI 提供；authorizedTenantId 来自已通过授权的服务端流程。
using (scopeFactory.CreateScope(authorizedTenantId))
{
    var order = await repository.GetAsync(orderId, cancellationToken);
}

// 必须先在宿主注册真实 ICrossTenantScopeAuthorizer，未注册或未授权会拒绝。
using (scopeFactory.CreateCrossTenantScope())
{
    var order = await repository.GetAsync(orderId, cancellationToken);
}
```

`ICrossTenantScopeAuthorizer.Authorize(AsgardIdentitySnapshot snapshot)` 同步验证可信服务端主体与权限，成功正常返回，拒绝抛 `UnauthorizedAccessException`。不要注册全部通过的实现，不阻塞网络授权调用，不把客户端传入的授权器/布尔值作为权限。

- `CreateScope(Guid.Empty)` 非法；无范围的仓储租户读取在缓存/数据库访问前拒绝
- 用 `using` 保证异常退出也恢复完整身份快照；在同一范围内构造并执行操作，不跨范围复用高级 joined-write/内嵌子查询构造器
- `snapshot with { TenantId = ... }` 清除跨租户能力；JSON 不能提升 `TenantAccess`
- 手动 FreeSql 装配调用 `AsgardTenantDataProtection.Configure(fsql, identityContext)`；仓储与数据库共用同一个环境 `IAsgardIdentityContext`，其 AsyncLocal 按执行流区分身份。不要为每个仓储另建身份对象
- 普通 `AddDatabase` 自动装配；即使没有身份服务，数据库也不默认放行租户数据
- 不增加 JWT claim 来绕过范围；业务 ACL、用户级过滤仍需在缓存命中路径执行同等检查，否则禁用该类实体缓存

## 写入与失效

- 单租户写入可回填空归属，拒绝其他租户；跨租户写入必须明确提供合法非空 `TenantId`
- 常规更新不可改变 `TenantId`，显式 `Set/SetRaw` 也不能绕过。所有权迁移需单独设计授权、事务与失效协议
- 单租户范围禁止 upsert，显式选择受保护的插入或更新
- 实体插入/更新/删除、主键删除、允许的 upsert 统一失效实体键；谓词删除保留集合 SQL 语义，按类型与固定表前缀失效
- 直接 FreeSql 标准写入虽有租户保护，但不自动调用仓储缓存失效；有缓存的数据经受支持仓储入口修改，或实现同等失效协议
- 原始 ADO SQL、主动 `DisableGlobalFilter`、原生批量写入、`UpdateDiy`、自定义级联等是可信基础设施入口。不得向不可信业务层暴露；调用者承担授权和一致性责任

## 工作单元与故障处理

使用受支持的 FreeSql `IUnitOfWork`：事务内不读取/填充共享实体缓存，写入不提前失效，真正提交后通过 `EntityChangeReport.OnChange` 统一失效；回滚不失效。

- 在写入前设置应用 `OnChange` 回调，或用 `+=` 追加；写入后直接覆盖会移除已登记的失效处理器
- 多仓储共享同一工作单元去重登记失效；应用回调仍应幂等，混合谓词删除/实体写入可能重复报告集合变化
- 若数据库已提交、缓存失效或原有回调失败，提交会抛带“数据库已提交”说明的 `AggregateException`。不能当作回滚，也不能盲目重试整笔事务；按业务操作记录修复/重试失效
- 未绑定受支持工作单元的 ADO 当前线程事务、`TransactionScope` 等外部事务，缓存启用时写入被拒绝。改用 FreeSql UnitOfWork 或统一关闭整个数据源缓存，不能仅禁用一个写入仓储而保留其他读取缓存
- cache-aside 仍有旧值回填竞态；本补丁没有 outbox，提交后崩溃窗口也仍存在。强一致业务需专门协议或统一关闭缓存

## 迁移与验证

1. 保留现有配置，核对缓存范围与逻辑业务数据库/schema 一致
2. 为后台任务、MQ、平台流程、公开入口补齐明确范围；真实授权器由业务实现
3. 修复历史空、无效或非规范 Guid D 格式的 TenantId；检查单列主键、固定表映射、自定义键覆盖和低层写入
4. 停止旧节点再升级，不能混跑旧分段实体键节点。v2 不读旧实体键，旧键受控清理或自然过期
5. 运行目标源码的范围、缓存和事务测试，再验证业务 ACL；FreeSql 3.5.311 保护读取 `_source` / `_asTablePriv` 等实现细节，升级依赖必须复测，不能静默跳过校验

详细实现与限制以 Asgard 的 `src/doc/31-单实体共享缓存与租户范围迁移.md`、`ExplicitTenantScopeTests` 和数据库/仓储测试为准。
