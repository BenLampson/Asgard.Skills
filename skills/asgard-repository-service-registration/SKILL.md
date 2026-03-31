---
name: asgard-repository-service-registration
description: Asgard 仓储与服务注册 skill。Use when implementing repositories, scanning assemblies, using RepositoryAttribute, AddRepositories, service registration conventions, PluginConventions, or deciding whether to use explicit DI versus service scanners in Asgard.
---

# Asgard 仓储与服务注册

## 作用

本模块用于标记、扫描并自动注册仓储类与业务服务类，遵循约定大于配置原则。

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
| 2. 手动批量注册 | `services.AddRepositories(typeof(EntryType).Assembly)` |
| 3. 插件中注册 | 使用 `context.AddPluginConventions<TPlugin, TConfig>()` 一键完成 |

**代码示例 - 定义仓储：**

```csharp
namespace {Namespace}.Repositories;

[Repository]
public class {EntityName}Repository : AbsAsgardRepositoryBase<{EntityName}, {KeyType}>
{
    public {EntityName}Repository(IFreeSql fsql, IMultiLevelCache cache, ILogger<{EntityName}Repository> logger)
        : base(fsql, cache, logger)
    {
    }
}
```

**代码示例 - 手动注册：**

```csharp
// 扫描当前程序集中所有 [Repository] 标记的类
_ = services.AddRepositories(typeof(Program).Assembly);
```

**代码示例 - 插件约定注册：**

```csharp
// 自动扫描仓储+服务，并加载插件配置
var config = context.AddPluginConventions<{PluginName}Plugin, {PluginConfig}Config>();
```

### 业务服务注册

| 场景 | 推荐做法 |
|------|----------|
| 少量服务 | 显式 `services.AddScoped/AddSingleton/AddTransient` |
| 模块批量扫描 | 使用 `[Service]` 特性 + `services.AddServices(assembly)` |
| 插件模块 | 交给 `AddPluginConventions` 自动处理 |

**代码示例 - 定义服务：**

```csharp
namespace {Namespace}.Services;

[Service]
public interface I{ServiceName}Service
{
}

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
- **扫描范围**：只扫描当前模块程序集，不扫描整个解决方案

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
- ❌ 不要把控制器直接当仓储用，不要让控制器写 SQL
- ❌ 不要扫描范围过大（比如扫描 `Asgard.Common`），导致无关类型被注入
- ❌ 不要让仓储包含业务逻辑，不要让服务包含 SQL 查询
