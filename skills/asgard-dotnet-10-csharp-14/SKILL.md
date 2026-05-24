---
name: asgard-dotnet-10-csharp-14
description: Asgard .NET 10 / C# 14 coding conventions skill. This is the mandatory coding-rules authority for Asgard. Use when writing any C# code for Asgard framework, following required .NET 10 / C# 14 conventions, comments, file rules, dependency injection patterns, testing expectations, Serilog infrastructure, or FreeSql-backed database logging conventions.
---

# Asgard .NET 10 / C# 14 Coding Conventions

## 作用

本 skill 定义了 Asgard 框架下编写 .NET 10 / C# 14 代码时必须遵循的编码规范和最佳实践。包括语言特性使用、基础设施模式、通用后端安全编码、测试、反模式避免、推荐类库等内容。

**重要**：

- 这是 Asgard 唯一的强制编码规则权威
- 其他 skill 只能引用本 skill，不能改写、放宽、忽略或给出冲突建议
- Asgard 使用传统 `Controller` 开发 Web API，不使用 Minimal API。接口层请参考 `$asgard-api-development`
- 项目结构请参考 `$asgard-plugin-structure`
- 需要对后端改动做复查、守门、踩坑排查时，请启用 `$asgard-backend-guard`

## 什么时候使用

- **编写任何新代码时** - 确保遵循 C# 14 语法和 Asgard 约定
- **重构现有代码** - 将旧语法升级为新标准
- **添加依赖注入** - 遵循生命周期约定
- **编写通用安全相关后端代码** - 遵循 API token 校验、密钥保护、CORS 等服务端实践
- **编写集成测试** - 使用 WebApplicationFactory 正确模式

以下内容不属于本 skill 的主职责，请改用对应 skill：

- Web 前端登录流、OIDC、PKCE、IDP 接入：`$identity-integration`
- Controller / VO 对外 API 契约、`long` / `ulong` 前端字符串输出规则：`$asgard-api-development`
- `AbsAsgardUserInfo` 与 claim 契约：`$asgard-identity-userinfo`
- `AsgardAuth` 授权 DSL：`$asgard-auth-authorization`

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
- Asgard 数据库日志统一走 `LogConfig.Database` + Serilog + 独立 `IFreeSql` + `Channel` 批量写入，并在批量插入成功后按 `RetentionDays` + `CleanupIntervalMinutes` 节流清理旧日志
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

## TS Gen 使用约定

当开发人员需要为前端生成 Asgard Controller 对应的 TypeScript 客户端时，统一使用 `Asgard.TsGen` 工具。

### 生成前提

- 只有继承 `ControllerBase` 且显式标记 `[AsgardTsGen]` 的控制器才会被扫描和生成
- 未标记该特性的控制器不会进入生成结果
- 控制器返回值仍应遵循 Asgard 统一包装约定，例如 `Response<T>`、`PageResponse<T>`、`CursorResponse<T>` 或 SSE
- 在 Yggdrasil 宿主内通过 `/asgard-tsgen` 导出时，只会导出**当前宿主已经加载的插件程序集**中、且已被 MVC 真实发现到的控制器
- 宿主不会导出未加载插件、宿主自身控制器，或虽在程序集里但未进入 MVC ApplicationPart 的控制器

### 典型用法

```powershell
dotnet run --project Common/Asgard.TsGen/Asgard.TsGen.csproj -- --assembly ./Host/Asgard.Yggdrasil.AspNetCore/bin/Debug/net10.0/Asgard.Yggdrasil.AspNetCore.dll
```

也可以在安装为工具后执行：

```powershell
asgard-tsgen --assembly ./bin/Debug/net10.0/MyApi.dll
```

开发环境下，如果使用 Yggdrasil 宿主内置导出端点，访问地址为：

```text
http://127.0.0.1:5000/asgard-tsgen
```

实际端口以宿主启动日志中的告警输出为准。宿主会在启动后打印完整访问地址，并在收到导出请求时输出当前插件程序集、MVC 已发现控制器以及最终命中的 TS 导出控制器，便于排查“只生成 common、不生成 controller/models”的问题。

### 输出规则

- 默认输出目录就是执行命令时所在的当前目录
- 生成器会重建自己负责的产物目录，当前固定为 `common/`、`controller/`、`models/`
- 这些目录应视为纯生成目录，不要手写或混入自定义代码
- 如果需要隔离生成结果，请先进入专门的前端客户端目录，再执行生成命令

### 团队约定

- 想让某个 API 暴露给前端生成客户端时，先为控制器添加 `[AsgardTsGen]`
- 修改控制器路由、参数或返回模型后，应重新执行一次 `TS Gen`
- 前端代码应信任最新生成结果，不要继续引用已被删除的旧接口文件
- 如果宿主导出结果只出现 `common/`，优先检查：插件是否真的已加载、控制器是否被 MVC 发现、控制器是否显式标记 `[AsgardTsGen]`

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

这些规则属于**必须遵守**的硬约束，不允许其他 skill 自行覆盖。

## Asgard 默认更新策略

以下规则属于 Asgard 项目生成服务层更新代码时**必须优先遵守**的默认策略，不是建议项：

### 适用范围

- 只要实体继承 `AbsAsgardBaseEntity`
- 或实体继承 `AbsAsgardTenantEntity`
- 或实体继承 `AbsAsgardTenantUserDataEntity`
- 或实体存在 `Version` 字段并标记 `[Column(IsVersion = true)]`

以上任一条件成立，都必须视为启用了 FreeSql 乐观锁，更新路径必须采用“先查后改”。

### 硬规则

- `Create` 场景可以使用 `dto.ToEntity()`
- `Update` 场景默认**禁止**使用 `dto.ToEntity()` 后直接 `UpdateAsync(entity)`
- 更新前必须先从数据库读取当前实体
- 必须在数据库读取出的原始实体上应用 DTO 中允许修改的字段
- 最后再执行 `UpdateAsync(entity)`
- `DTO` 不是 `Version` 的可信来源，乐观锁版本必须来自数据库当前实体
- 不允许让前端或 DTO 决定 `CreateTime`、`CreateBy`、`Deleted`、租户归属字段、客户端归属字段或其他持久化标识字段
- 对租户实体，更新时不允许随 DTO 覆盖 `TenantId` 等归属字段；如果业务明确允许，必须在代码中加中文注释说明原因和边界

### 实现要求

- 如果实体提供了 `Update(...)`、`Enable()`、`Disable()` 等行为方法，优先调用实体方法承接状态变更
- 如果实体没有行为方法，再在服务层显式逐字段赋值
- 逐字段赋值完成后，如果实体约定需要调用 `MarkAsUpdated()`，必须显式调用
- 遇到 `UpdateAsync(string id, XxxDto dto, ...)` 这类签名时，要主动警惕乐观锁问题，优先检查实体继承链和 `Version` / `IsVersion = true` 标记
- 如果没有特别说明，**不要生成**“DTO 重建实体后直接更新”的代码

### 反模式与推荐模式

❌ 反模式：

```csharp
var entity = dto.ToEntity();
entity.Id = id;
await repository.UpdateAsync(entity);
```

✅ 推荐模式：

```csharp
var entity = await repository.GetByIdAsync(id)
    ?? throw new InvalidOperationException($"未找到实体：{id}");

entity.Update(...);
await repository.UpdateAsync(entity);
```

如果没有 `Update(...)` 行为方法，则改为先查询实体，再显式逐字段赋值，并在需要时调用 `MarkAsUpdated()`。

完整规则见 `references/project_rules.md` 和 `references/never-do-this.md`。

## 推荐参考资料

所有详细内容都在 `references/` 目录：
- `csharp-14.md` - C# 14 语言特性
- `infrastructure.md` - 依赖注入、配置、缓存、弹性
- `security.md` - 安全最佳实践清单
- `testing.md` - 集成测试示例
- `anti-patterns.md` - 反模式对照表
- `libraries.md` - 推荐类库和示例
- `project_rules.md` - 项目规则
- `never-do-this.md` - 禁忌列表

代码范本请参考 `templates/` 目录：
- `PrimaryConstructor.cs.template` - 主构造函数模板
- `ExtensionBlock.cs.template` - 扩展块模板
- `MediatRCommand.cs.template` - MediatR 命令模板
