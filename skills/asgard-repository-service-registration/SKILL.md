---
name: asgard-repository-service-registration
description: Asgard 仓储与服务注册 skill。Use when implementing repositories, scanning assemblies, using RepositoryAttribute, AddRepositories, service registration conventions, PluginConventions, or deciding whether to use explicit DI versus service scanners in Asgard.
---

# Asgard 仓储与服务注册

## 作用

本模块用于标记、扫描并自动注册仓储类与业务服务类，遵循约定大于配置原则。

结构与规则边界：

- 仓储接口默认放在 `Domains/IRepositories`
- 仓储实现默认放在 `Domains/Repositories`
- 服务接口默认放在 `Services/IServices`
- 服务实现默认放在 `Services/Services`
- 项目结构见 `$asgard-plugin-structure`
- 编码硬规则见 `$asgard-dotnet-10-csharp-14`
- 乐观锁与更新路径规则见 `$asgard-database`
- 需要对仓储/服务层改动做复查时，请启用 `$asgard-backend-guard`

什么时候使用本 skill：
- 实现新的数据库仓储类时
- 在独立模块中批量注册多个服务时
- 在插件中按照约定自动扫描注册时
- 需要决定使用显式 DI 注册还是约定扫描时

## 核心约定

### 仓储注册

| 步骤 | 做法 |
|------|------|
| 1. 标记仓储类 | 添加 `[Repository]` 特性 |
| 2. 基类继承 | 统一继承 `AbsAsgardRepositoryBase<TEntity, TKey>`，并注入 `IMultiLevelCache`、`ILogger<TRepository>`、`IAsgardRepositoryContext` |
| 3. 插件中注册 | 使用 `context.AddPluginConventions<TPlugin, TConfig>()` 一键完成 |
| 3. 非插件或特殊扫描范围 | 才手动调用 `services.AddRepositories(typeof(EntryType).Assembly)` |

**代码示例 - 定义仓储：**

```csharp
namespace {Namespace}.Domains.Repositories;

[Repository]
public class {EntityName}Repository : AbsAsgardRepositoryBase<{EntityName}, {KeyType}>, I{EntityName}Repository
{
    public {EntityName}Repository(
        IFreeSql fsql,
        IMultiLevelCache cache,
        ILogger<{EntityName}Repository> logger,
        IAsgardRepositoryContext repositoryContext)
        : base(fsql, cache, logger, repositoryContext)
    {
    }
}
```

**租户仓储补充约定：**

- 如果实体继承 `AbsAsgardTenantEntity`，默认查询路径依赖 FreeSql 全局过滤自动附加当前租户条件
- 仓储构造函数必须注入 `IMultiLevelCache cache` 并传给 `base(...)`，不要因为业务代码没有显式使用缓存就省略
- 仓储构造函数里应注入 `IAsgardRepositoryContext`，让 `AbsAsgardRepositoryBase` 统一获取身份、追踪与分布式锁入口
- 即使 `caching.enabled: false`，Yggdrasil 也会注册可注入的空 `IMultiLevelCache`，仓储构造函数不需要为“禁用缓存”分支改写
- 业务服务和控制器不需要重复计算默认租户过滤，除非场景明确要求跨租户访问

**代码示例 - 手动注册（仅非插件或特殊扫描范围）：**

```csharp
// 扫描当前程序集中所有 [Repository] 标记的类
_ = services.AddRepositories(typeof(Program).Assembly);
```

**代码示例 - 插件约定注册：**

```csharp
// 自动扫描仓储+服务，并加载插件配置
var config = context.AddPluginConventions<{PluginName}Plugin, {PluginConfig}Config>();
```

插件项目默认只能保留上面的 `AddPluginConventions`。除非要扫描插件程序集之外的额外程序集，否则不要同时再写 `AddRepositories(...)` 或 `AddServices(...)`。

### 业务服务注册

| 场景 | 推荐做法 |
|------|----------|
| 少量服务 | 显式 `services.AddScoped/AddSingleton/AddTransient` |
| 模块批量扫描 | 使用 `[Service]` 特性 + `services.AddServices(assembly)` |
| 插件模块 | 交给 `AddPluginConventions` 自动处理 |

插件模块中，显式 DI 只用于 `AddPluginConventions` 覆盖不了的对象，例如 SignalR、第三方 SDK client、HostedService、特殊单例、跨程序集能力或需要自定义生命周期的服务。普通业务服务只要带 `[Service]` 并在插件程序集内，就不要再手写注册。

**代码示例 - 服务接口与实现必须分文件：**

接口文件：`Services/IServices/I{ServiceName}Service.cs`

```csharp
namespace {Namespace}.Services.IServices;

public interface I{ServiceName}Service
{
}
```

实现文件：`Services/Services/{ServiceName}Service.cs`

```csharp
namespace {Namespace}.Services.Services;

[Service]
public class {ServiceName}Service : I{ServiceName}Service
{
    private readonly I{RepositoryName}Repository _{repositoryNameCamel};

    public {ServiceName}Service(I{RepositoryName}Repository {repositoryNameCamel})
    {
        _{repositoryNameCamel} = {repositoryNameCamel};
    }
}
```

## 职责分离原则

- **仓储层**：只做数据访问（CRUD、查询）
- **业务服务层**：做跨仓储编排、事务、缓存调用
- **控制器**：只调用业务服务，不直接访问仓储
- **插件扫描入口**：插件中默认只调用 `AddPluginConventions<TPlugin, TConfig>()`
- **扫描范围**：只扫描当前模块程序集，不扫描整个解决方案；跨程序集扫描必须有明确理由
- **目录归属**：仓储与服务默认按结构 skill 的目录归位
- **租户隔离**：默认由框架仓储基类和 FreeSql 过滤器统一承接，不要让每个仓储各自实现一套
- **默认更新策略**：乐观锁实体必须采用“先查后改”，不要在服务层把 DTO 重建成新实体后直接更新

## 服务层更新硬规则

对于继承 `AbsAsgardBaseEntity`、`AbsAsgardTenantEntity`、`AbsAsgardTenantUserDataEntity` 的实体服务，生成更新代码时必须遵守以下要求：

- 遇到 `UpdateAsync(string id, XxxDto dto, ...)` 之类的方法签名时，先检查实体是否带 `Version` / `[Column(IsVersion = true)]`
- 只要存在乐观锁字段，就必须先从仓储读取当前实体
- 优先调用实体自己的 `Update(...)`、`Enable()`、`Disable()` 等行为方法
- 如果没有行为方法，再在服务层显式逐字段赋值
- 如果实体约定需要更新时间或审计标记，赋值后显式调用 `MarkAsUpdated()`
- 禁止让 DTO 覆盖 `CreateTime`、`CreateBy`、`Deleted`、`TenantId`、`ClientId` 等持久化字段

推荐模式：

```csharp
var entity = await repository.GetByIdAsync(id)
    ?? throw new InvalidOperationException($"未找到实体：{id}");

entity.Update(...);
await repository.UpdateAsync(entity);
```

反模式：

```csharp
var entity = dto.ToEntity();
entity.Id = id;
await repository.UpdateAsync(entity);
```

## 代码模板

完整的模板文件见 `templates/` 目录：
- `Repository.cs.template` - 仓储类模板
- `Service.cs.template` - 业务服务模板
- `AddRepositories_Manual.cs.template` - 手动注册模板
- `AddPluginConventions.cs.template` - 插件约定注册模板

AI 生成代码时，建议套用这些模板保持风格一致。

## 参考源码

需要查看接口定义或实现细节时，阅读 `references/` 目录：
- `RepositoryAttribute.cs` - 仓储标记特性
- `RepositoryServiceCollectionExtensions.cs` - `AddRepositories` 扩展方法
- `PluginConventions_AddPluginConventions.cs` - 插件约定注册方法

## 不要这样做

- ❌ 不要默认 `ServiceScanner` 已经在所有宿主路径中自动接线
- ❌ 不要在插件项目里既调用 `AddPluginConventions<TPlugin, TConfig>()`，又重复调用 `AddRepositories(typeof(TPlugin).Assembly)` / `AddServices(typeof(TPlugin).Assembly)`
- ❌ 不要为每个仓储、每个业务服务逐条写 `AddScoped`，只要它们符合 `[Repository]` / `[Service]` 插件约定
- ❌ 不要把控制器直接当仓储用，不要让控制器写 SQL
- ❌ 不要扫描范围过大（比如扫描 `Asgard.Common`），导致无关类型被注入
- ❌ 不要让仓储包含业务逻辑，不要让服务包含 SQL 查询
- ❌ 不要自行定义另一套仓储/服务目录规则
- ❌ 不要绕开 `AbsAsgardRepositoryBase` 直接 new `BaseRepository` 作为默认仓储实现
- ❌ 不要省略仓储构造函数里的 `IMultiLevelCache`
- ❌ 不要在每个仓储方法里复制粘贴 `TenantId` 条件，默认租户过滤应交给框架统一处理
- ❌ 不要在服务层把 DTO 重建成新实体后直接 `UpdateAsync`，这会破坏乐观锁版本和持久化字段
