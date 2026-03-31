---
name: asgard-dotnet-10-csharp-14
description: Asgard .NET 10 / C# 14 coding conventions skill. Use when writing C# 14 code for Asgard framework, following modern .NET conventions including primary constructors, extension blocks, file-scoped namespaces, Minimal APIs, dependency injection patterns, resilience, security, and testing.
---

# Asgard .NET 10 / C# 14 Coding Conventions

## 作用

本 skill 定义了 Asgard 框架下编写 .NET 10 / C# 14 代码时必须遵循的编码规范和最佳实践。包括语言特性使用、Minimal API、基础设施模式、安全、测试、反模式避免、推荐类库等内容。

## 什么时候使用

- **编写任何新代码时** - 确保遵循 C# 14 语法和 Asgard 约定
- **重构现有代码** - 将旧语法升级为新标准
- **处理 Minimal API** - 使用端点分组、过滤器、Typed Results
- **添加依赖注入** - 遵循生命周期约定
- **编写安全相关代码** - 遵循认证授权最佳实践
- **编写集成测试** - 使用 WebApplicationFactory 正确模式

## C# 14 语言特性使用指南

### 优先使用新语法

| 特性 | 使用场景 | 示例 |
|------|----------|------|
| **Primary Constructors** | 依赖注入注入、简单类型 | `public class UserService(IOptions<Settings> settings, ILogger<UserService> logger)` |
| **Collection Expressions** | 数组、列表、字典初始化 | `int[] numbers = [1, 2, 3];` |
| **`field` keyword** | 自动属性带逻辑 | `set => field = value.Trim();` |
| **Extension Blocks** | 扩展方法组织 | `public static extension IQueryableExtensions on IQueryable<T>` |
| **File-scoped Namespaces** | 减少嵌套 | `namespace MyFeature;` |
| **Nullable Reference Types** | 空安全 | `string?`, `null!`, `ArgumentNullException.ThrowIfNull` |
| **Null Conditional Assignment** | 简写条件赋值 | `user?.Name = "John";` |

### 代码示例：主构造函数

```csharp
namespace {Namespace};

/// <summary>
/// {ServiceSummary}
/// </summary>
public class {ServiceName}(
    IOptions<{SettingsName}> settings,
    ILogger<{ServiceName}> logger,
    AbsAsgardContext asgardContext)
{
    private readonly {SettingsType} _settings = settings.Value;
    private readonly ILogger<{ServiceName}> _logger = logger;
    protected readonly AbsAsgardContext AsgardContext = asgardContext;
}
```

### 代码示例：扩展块

```csharp
namespace {Namespace};

public static extension {ExtensionName} on {TargetType}<{GenericParameter}>
{
    /// <summary>
    /// {MethodSummary}
    /// </summary>
    public async Task<List<TResult>> {MethodName}<TResult>(
        this {TargetType}<{GenericParameter}> query,
        int page,
        int pageSize)
    {
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// 条件 where 子句
    /// </summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }
}
```

## Minimal API 最佳实践

### 核心约定

| 概念 | 优先选择 | 避免 |
|------|----------|------|
| Results | `TypedResults` | `Results` |
| 组织方式 | 按特性分组 + 扩展方法 | 所有端点放在 Program.cs |
| 验证 | 端点过滤器 + FluentValidation | 每个端点手动验证 |
| 弹性 | `AddStandardResilienceHandler()` | 手动 Polly 配置 |
| 文档 | `WithOpenApi()`, `WithTags()`, `Produces()` | 缺失文档 |

### 代码示例：路由分组

```csharp
namespace {Namespace};

/// <summary>
/// {FeatureName} endpoints mapping
/// </summary>
public static class {FeatureName}Endpoints
{
    /// <summary>
    /// Map all {FeatureName} endpoints
    /// </summary>
    /// <param name="routes">Endpoint route builder</param>
    public static void Map{FeatureName}(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/{RoutePrefix}")
            .WithTags("{FeatureName}")
            {AddMetadata};

        group.MapGet("/", GetAll{EntityName});
        group.MapGet("/{id}", Get{EntityName}ById);
        group.MapPost("/", Create{EntityName});
        group.MapPut("/{id}", Update{EntityName});
        group.MapDelete("/{id}", Delete{EntityName});
    }

    /// <summary>
    /// Get all {EntityName}
    /// </summary>
    private static async Task<IResult> GetAll{EntityName}(
        {DependencyInjection})
    {
        var result = await service.GetAllAsync();
        return TypedResults.Ok(result);
    }
}
```

在 `Program.cs` 中调用：

```csharp
app.Map{FeatureName}();
```

## 依赖注入与基础设施

### 生命周期对照表

| 生命周期 | 使用场景 |
|----------|----------|
| **Singleton** | 有状态对象，应用生命周期内存活 |
| **Scoped** | 每个请求服务，数据库上下文 |
| **Transient** | 轻量无状态服务，每次使用创建 |

### 关键模式

- 总是给选项配置加上 `.ValidateOnStart()`
- 结构化日志使用占位符，不使用字符串插值
- HttpClient 总是通过 `IHttpClientFactory` 注入
- HttpClient 总是加上 `AddStandardResilienceHandler()`
- 后台任务使用 `BackgroundService`
- 生产者消费者队列使用 `System.Threading.Channels`

## 安全最佳实践

- 始终使用 HTTPS/HSTS
- 从不把密钥提交到 Git
- 总是参数化 SQL 查询避免注入
- 使用 DTO 防止批量赋值
- 配置 CORS 指定具体来源
- 使用 ASP.NET Core 内置密码哈希
- 添加安全响应头

完整检查表见 `references/security.md`。

## 测试最佳实践

- 使用 `WebApplicationFactory<Program>` 做集成测试
- 在测试中用内存数据库替换真实数据库
- 自定义认证测试用 `TestAuthHandler`
- 使用 FluentAssertions 做断言
- 使用 `IAsyncLifetime` 做异步初始化/清理

完整示例见 `references/testing.md`。

## 推荐类库

| 类库 | 用途 | NuGet |
|------|------|------|
| MediatR | CQRS / Mediator | `MediatR` |
| FluentValidation | 验证规则 | `FluentValidation.DependencyInjectionExtensions` |
| Mapster | 对象映射 | `Mapster.DependencyInjection` |
| ErrorOr | Result 模式 | `ErrorOr` |
| Polly | 弹性 | `Microsoft.Extensions.Http.Resilience` |
| Serilog | 结构化日志 | `Serilog.AspNetCore` |
| .NET Aspire | 云原生编排 | `Aspire.Hosting` |

完整示例见 `references/libraries.md`。

## 反模式对照表

| ❌ 反模式 | ✅ 替代方案 |
|-----------|------------|
| `new HttpClient()` | 注入 `HttpClient` 或 `IHttpClientFactory` |
| `Results.Ok()` | `TypedResults.Ok()` |
| 手动 Polly 配置 | `AddStandardResilienceHandler()` |
| `DateTime.Now` | `DateTime.UtcNow` |
| `GetAsync().Result` | `await GetAsync()` |
| 异常做流程控制 | `ErrorOr<T>` / Result 模式 |
| 手动后备字段 | C# 14 `field` 关键字 |
| 传统扩展方法类 | C# 14 扩展块 |
| 缺失 `ValidateOnStart()` | 总是加上 `.ValidateOnStart()` |
| Singleton 直接注入 Scoped | 使用 `IServiceScopeFactory` |
| `_count++` 在 Singleton | `Interlocked.Increment(ref _count)` |

完整反模式列表见 `references/anti-patterns.md`。

## Asgard 项目特定规则

| 规则 | 要求 |
|------|------|
| **文件编码** | UTF-8 |
| **行结束符** | CRLF |
| **注释覆盖率** | ≥ 80% |
| **注释语言** | 中文 |
| **每个文件** | 一个类 |
| **文件大小** | 不超过 400 行 |
| **空检查** | 使用 `XXXXException.ThrowIfNull()`，不手动 throw |
| **Global using** | 利用 Global using，减少重复 |

完整规则见 `references/project_rules.md` 和 `references/never-do-this.md`。

## 推荐参考资料

所有详细内容都在 `references/` 目录：
- `csharp-14.md` - C# 14 语言特性
- `minimal-apis.md` - Minimal API 最佳实践
- `infrastructure.md` - 依赖注入、配置、缓存、弹性
- `security.md` - 安全最佳实践清单
- `testing.md` - 集成测试示例
- `anti-patterns.md` - 反模式对照表
- `libraries.md` - 推荐类库和示例
- `project_rules.md` - 项目规则
- `never-do-this.md` - 禁忌列表

代码范本请参考 `templates/` 目录：
- `PrimaryConstructor.cs.template` - 主构造函数模板
- `EndpointRouteGroup.cs.template` - 端点路由分组模板
- `ExtensionBlock.cs.template` - 扩展块模板
- `MediatRCommand.cs.template` - MediatR 命令模板
