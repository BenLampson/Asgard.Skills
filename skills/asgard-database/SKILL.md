---
name: asgard-database
description: Asgard 数据库模块 skill。Use when configuring database.enabled, provider, connection strings, repositories, data-access structure, or explaining how Asgard database features integrate with repositories and services.
---

# Asgard 数据库模块

## 作用

本模块负责配置数据库连接，基于 FreeSQL ORM 框架提供数据访问能力。

当前仓库的数据库访问有两条必须统一遵守的约定：

- 仓储实现统一继承 `AbsAsgardRepositoryBase<TEntity, TKey>`，不要再自建另一套 FreeSql 仓储基类
- FreeSql 的租户隔离统一通过框架内置 `GlobalFilter` + 身份上下文完成，租户实体不要在每个查询里重复手写 `TenantId` 条件

当前仓库的更新路径还必须统一遵守一条硬规则：

- 只要实体继承 `AbsAsgardBaseEntity` / `AbsAsgardTenantEntity` / `AbsAsgardTenantUserDataEntity`，或存在 `Version` + `[Column(IsVersion = true)]`，更新时默认采用“先查后改”，禁止 `dto.ToEntity()` 后直接 `UpdateAsync(entity)`

结构与规则边界：

- 实体默认位于 `Models/Entities`
- 仓储接口默认位于 `Domains/IRepositories`
- 仓储实现默认位于 `Domains/Repositories`
- 项目结构见 `$asgard-plugin-structure`
- 编码硬规则见 `$asgard-dotnet-10-csharp-14`
- 需要对数据库相关实现做风险复查时，请启用 `$asgard-backend-guard`

什么时候使用本 skill：
- 启用数据库功能配置
- 新增数据库支持的业务模块
- 解释数据库配置与仓储分层的约定
- 调试数据库连接问题
- 设计或解释基于 FreeSql 的数据库日志存储能力

## 配置方式

在项目根目录 `app.yaml`（宿主）或 `plugin.yaml`（插件）中配置：

```yaml
database:
  # 是否启用数据库模块
  enabled: true
  # 数据库提供者
  # 支持: sqlserver, postgresql, mysql, sqlite, oracle, dm(达梦), kingbase(人大金仓)
  provider: mysql
  # 连接字符串
  connectionString: "Data Source=localhost;Database={db_name};User ID={user};Password={password};"
```

**配置注册示例：**

```csharp
// 宿主如果需要请求态租户过滤，先注册 Asgard ASP.NET Core 身份/租户能力
_ = builder.Services.AddAsgardAspNetCore();

// 从配置绑定并注册数据库服务
var dbConfig = builder.Configuration.Get<DatabaseConfig>();
_ = builder.Services.AddDatabase(dbConfig);
```

如果是 HTTP 宿主，还需要在认证后启用：

```csharp
app.UseAuthentication();
app.UseAsgardTenant();
app.UseAuthorization();
```

## 支持的数据库

| 提供者名称 | FreeSQL 类型 | 说明 |
|------------|--------------|------|
| `sqlserver` | SqlServer | Microsoft SQL Server |
| `postgresql` | PostgreSQL | PostgreSQL |
| `mysql` | MySql | MySQL/MariaDB |
| `sqlite` | Sqlite | SQLite 文件数据库 |
| `oracle` | Oracle | Oracle |
| `dm` | Dameng | 达梦数据库 |
| `kingbase / 人大金仓` | KingbaseES | 人大金仓 |

## 数据库日志约定

Asgard 的数据库日志不属于业务仓储层，而是日志基础设施的一部分。它虽然同样基于 FreeSql，但有一套独立边界：

- 数据库日志通过 `LogConfig.Database` 启用，不走 `AddDatabase(DatabaseConfig)` 注册路径
- 数据库日志必须自建独立 `IFreeSql`，不要复用业务主库 `IFreeSql`
- `logging.database.provider` 与主数据库 `database.provider` 使用同一套 provider 语义
- 启动时允许自动同步一次日志表结构
- 运行期使用 `Channel` 做异步批量落库，不在业务线程中直接插入数据库
- 停止时必须尽量冲刷尾批次日志，再释放日志数据库连接

配置示例：

```yaml
logging:
  database:
    enabled: true
    provider: mysql
    connectionString: "Server=localhost;Database=asgard_logs;Uid=root;Pwd=123456;"
    tableName: app_logs
    batchSize: 100
    period: 2
```

推荐字段：

- `Id`
- `Timestamp`
- `Level`
- `Message`
- `MessageTemplate`
- `Exception`
- `PropertiesJson`
- `TraceId`
- `SpanId`
- `MachineName`
- `ThreadId`

实现建议：

- `Message` 保存渲染后的最终文本，便于直接检索
- `MessageTemplate` 保存模板原文，便于结构化统计
- `PropertiesJson` 保存结构化属性展开后的 JSON
- `TraceId` / `SpanId` 优先从日志属性获取，没有时再回退到当前链路上下文
- 后台写入异常只记诊断日志，不反抛回业务线程

不要这样做：

- ❌ 不要把数据库日志表当业务实体表纳入仓储层
- ❌ 不要让日志写入复用租户过滤或业务仓储基类
- ❌ 不要在 `Emit` 或业务日志调用点同步写数据库
- ❌ 不要遗漏关闭阶段的 flush

## 代码组织分层

| 层级 | 职责 | 做法 |
|------|------|------|
| **实体层** | 原始数据库对象 | 放在 `Models/Entities` |
| **仓储层** | 数据访问、CRUD、查询 | 默认放在 `Domains/IRepositories` 与 `Domains/Repositories`，实现类继承 `AbsAsgardRepositoryBase<TEntity, TKey>`，加 `[Repository]` 特性，并注入 `IAsgardRepositoryContext` 以统一接入租户、追踪与分布式锁能力 |
| **业务服务层** | 跨仓储编排、事务、业务逻辑 | 注入多个仓储，处理业务流程 |
| **控制器层** | API 入口 | 只调用业务服务，不直接访问仓储 |

**仓储定义示例：**

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

### 多租户约定

- 查询、更新、删除：只要实体继承 `AbsAsgardTenantEntity`，FreeSql 会通过框架注册的 `GlobalFilter` 自动带当前租户条件
- 新增、更新：如果租户实体的 `TenantId` 为空，`AbsAsgardRepositoryBase` 会通过 `IAsgardRepositoryContext.IdentityContext` 自动回填当前租户
- HTTP 请求：租户值来自 `UseAsgardTenant()` 写入的请求身份上下文
- 后台任务：租户值来自 `ITenantScopeFactory.CreateScope(tenantId)` 创建的作用域
- 平台级流程：当前租户为空时，不会附加租户过滤，也不会强行写入 `TenantId`

推荐做法：

- 租户实体统一继承 `AbsAsgardTenantEntity`
- 仓储统一使用框架仓储基类，不要在每个方法里重复 `Where(x => x.TenantId == ...)`
- 只有在明确需要跨租户或禁用过滤时，才在非常局部的位置做特殊处理，并补注释说明原因

### 乐观锁实体更新规范

Asgard 项目的大多数实体基类默认带有 `Version` 和 `[Column(IsVersion = true)]`，因此更新代码必须把乐观锁当作默认前提，而不是可选项。

硬规则：

- `Create` 场景可以使用 `dto.ToEntity()`
- `Update` 场景默认禁止使用 `dto.ToEntity()` 后直接更新
- 更新时必须先查询数据库当前实体，再在原实体上应用允许修改的字段
- `Version` 必须来自数据库当前实体，不能信任前端或 DTO 提供的值
- `CreateBy`、`CreateTime`、`Deleted`、`TenantId`、`ClientId` 等持久化字段不能在更新时被 DTO 覆盖
- 对租户实体，不允许在更新路径中随 DTO 改写租户归属字段；只有业务明确允许时，才能局部放开并补中文注释

推荐模式：

```csharp
var entity = await repository.GetByIdAsync(id)
    ?? throw new InvalidOperationException($"未找到实体：{id}");

entity.Update(...);
await repository.UpdateAsync(entity);
```

如果实体没有行为方法，则显式逐字段赋值，并在约定需要时调用 `MarkAsUpdated()`。

反模式：

```csharp
var entity = dto.ToEntity();
entity.Id = id;
await repository.UpdateAsync(entity);
```

## 注册方式

| 场景 | 推荐做法 |
|------|----------|
| 独立模块（非插件）| `services.AddRepositories(typeof(EntryType).Assembly)` |
| 插件模块 | `context.AddPluginConventions<TPlugin, TConfig>()` 自动扫描 |
| 单个服务 | 显式 `AddScoped<IRepository, RepositoryImpl>` |

## 代码模板

完整模板见 `templates/` 目录：
- `appyaml.database.yaml.template` - YAML 配置模板
- `Program_AddDatabase.cs.template` - 注册数据库服务模板
- `RepositoryInheritance.cs.template` - 仓储类继承模板

## 参考源码

需要查看接口定义时读 `references/`：
- `DatabaseConfig.cs` - 数据库配置类定义
- `DatabaseServiceCollectionExtensions.cs` - `AddDatabase` 扩展方法
- `RepositoryInheritance.cs.template` - 标准仓储继承方式

## 不要这样做

- ❌ 不要在数据库模块禁用时继续假设 `IFreeSql` 一定能注入
- ❌ 不要把 SQL/ORM 访问直接写进控制器
- ❌ 不要在业务代码里到处 `if (provider == ...)` 判断，让提供者来自配置
- ❌ 不要为同一模块建立多套不一致的数据访问入口
- ❌ 不要把连接字符串硬编码在代码里，通过配置覆盖
- ❌ 不要自行定义另一套实体或仓储目录结构
- ❌ 不要为租户实体重复手写 `TenantId` 过滤作为默认路径，这会和框架全局过滤割裂
- ❌ 不要省略仓储构造函数里的 `IAsgardRepositoryContext`，否则仓储无法统一获得租户回填、链路追踪与分布式锁入口
- ❌ 不要对乐观锁实体使用 `dto.ToEntity()` 后直接 `UpdateAsync(entity)`，这会丢失数据库当前 `Version` 并覆盖持久化字段
