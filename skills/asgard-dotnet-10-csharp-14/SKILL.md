---
name: asgard-dotnet-10-csharp-14
description: "编写、修改或审查 Asgard C# 代码时使用的编码规则权威：.NET 10/C# 14、文件规范、中文注释、DI 和乐观锁更新。API 与项目结构另用专项 skill。"
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
- 或实体继承任一 `AbsAsgard*SoftDeleteAuditedEntity`
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

## C# 14 语言特性使用指南

编写主构造函数或扩展块时，读取 [语言示例](references/language-examples.md)；其他语言细节按需读取 [C# 14 参考](references/csharp-14.md)。

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
- 密码哈希使用 Asgard `PasswordHasher`，具体契约读取 `$asgard-security`
- 添加安全响应头

完整检查表见 `references/security.md`。

## 测试最佳实践

- 使用 `WebApplicationFactory<Program>` 做集成测试
- 测试替身按被测行为选择；FreeSql 租户过滤、乐观锁和数据库方言需要匹配的数据库验证
- 自定义认证测试用 `TestAuthHandler`
- 使用 FluentAssertions 做断言
- 使用 `IAsyncLifetime` 做异步初始化/清理

新增或更新单元测试时读取 `$dotnet-unit-testing`，使用 xUnit v3；集成测试示例按需读取 `references/testing.md`，并对照当前测试 SDK。

## TS Gen 使用约定

仅项目选择 TsGen 且任务涉及客户端生成时，读取 [TsGen 使用约定](references/tsgen-usage.md)。

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
| Controller 返回裸 DTO / VO | 使用统一 `Response<T>` / 分页响应 |
| 手动 Polly 配置 | `AddStandardResilienceHandler()` |
| `DateTime.Now` | `DateTime.UtcNow` |
| `GetAsync().Result` | `await GetAsync()` |
| 异常做流程控制 | `ErrorOr<T>` / Result 模式 |
| 手动后备字段 | C# 14 `field` 关键字 |
| `public static extension ... on ...` | `static class` 内声明 `extension<T>(...)` |
| 缺失 `ValidateOnStart()` | 总是加上 `.ValidateOnStart()` |
| Singleton 直接注入 Scoped | 使用 `IServiceScopeFactory` |
| `_count++` 在 Singleton | `Interlocked.Increment(ref _count)` |

完整反模式列表见 `references/anti-patterns.md`。

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
