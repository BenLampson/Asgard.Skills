---
name: asgard-api-development
description: Asgard Web API 开发 skill。Use when creating or updating controllers, routes, unified Response models, pagination responses, exception handling, Swagger-facing API behavior, or controller code that should follow Asgard BaseController conventions.
---

# Asgard API Development

## 作用

用于开发遵循 Asgard 约定的 Web API 控制器。当你需要：
- 创建新的 API 控制器
- 修改现有的 API 接口
- 添加分页列表接口
- 统一 API 响应格式
- 集成异常处理
- 遵循框架约定编写控制器代码

结构与规则边界：

- 控制器文件默认位于 `Controllers/`，目录权威见 `$asgard-plugin-structure`
- 输入模型默认位于 `Models/DTO`，输出模型默认位于 `Models/VO`
- 编码硬规则统一见 `$asgard-dotnet-10-csharp-14`

## 什么时候使用

- **需要创建控制器时** - 继承 `BaseController` 并遵循框架约定
- **需要统一响应格式时** - 使用 `Response<T>`、`PageResponse<T>`、`CursorResponse<T>`
- **需要列表分页时** - 使用标准页码分页或游标分页
- **需要异常处理时** - 启用全局异常处理中间件

## 核心约定

| 约定 | 说明 |
|------|------|
| **基类继承** | 必须继承 `BaseController`，不要直接继承 `ControllerBase` |
| **上下文注入** | 构造函数必须注入 `AbsAsgardContext` 并传给基类 |
| **响应统一** | 统一响应约束只作用于 Controller 对外返回；Controller 必须把最终 VO 包装成 `Response<T>`、`Response<object>`、`PageResponse<T>` 或 `CursorResponse<T>` 返回给前端 |
| **职责分离** | 控制器只做输入输出编排，业务逻辑放服务，数据访问放仓储 |
| **模型位置** | 输入 DTO 默认位于 `Models/DTO`，输出 VO 默认位于 `Models/VO` |
| **异常处理** | 启用 `UseAsgardExceptionHandler()` 全局处理，不要每个 Action 都写 try/catch |

## 强制要求

以下要求属于 Asgard Web API 的硬约束，不允许为了“方便”而放宽：

- 所有 Controller 必须继承 `BaseController`
- 分层职责固定为：`Controller -> Service -> Repository -> Entity`
- 输出职责固定为：`Service` 产出 DTO，`Controller` 把 DTO 转成 VO 后再统一包装响应
- 所有 Controller Action 对外返回值必须统一使用 `Response<T>`、`Response<object>`、`PageResponse<T>` 或 `CursorResponse<T>`
- 普通查询、详情、创建、修改、删除等接口默认返回 `Response<T>` 或 `Response<object>`
- 页码分页列表接口必须返回 `PageResponse<T>`
- 游标分页 / 无限滚动列表接口必须返回 `CursorResponse<T>`
- 不允许 Controller 直接返回未包装的 VO、DTO、字符串、布尔值、数字、匿名对象或集合
- 不允许在 Controller 中再自定义另一套通用响应壳模型
- Swagger / OpenAPI 的 `ProducesResponseType` 也必须与统一响应模型保持一致
- 对于返回实体详情或单一资源的 Action，如果成功返回类型是 `Response<TVo>`，则 `404 NotFound` 的 `ProducesResponseType` 也应优先标注为 `Response<TVo>`，保持 Swagger 文档与统一响应壳的泛型语义一致

## 响应方法对照表

| 方法 | 使用场景 | 返回类型 |
|------|----------|----------|
| `Success<T>(data)` | 普通成功响应，带数据 | `Response<T>` |
| `Success(message)` | 成功响应，无数据 | `Response<object>` |
| `SuccessPage(data, totalCount, page, size)` | 标准页码分页 | `PageResponse<TItem>` |
| `SuccessCursor(data, hasMore, nextCursor, lastId)` | 游标分页（无限滚动） | `CursorResponse<TItem>` |
| `Fail<T>(code, message)` | 失败响应，自定义状态码 | `Response<T>` |
| `BadRequest<T>(message)` | 参数错误 | `Response<T>` |
| `NotFound<T>(message)` | 资源不存在 | `Response<T>` |
| `ServerError<T>(message)` | 服务器内部错误 | `Response<T>` |

## 代码示例

### 基础控制器

```csharp
namespace {Namespace}.Controllers;

/// <summary>
/// {ControllerSummary}
/// </summary>
public class {ControllerName} : BaseController
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asgardContext">Asgard 上下文</param>
    /// <param name="{ServiceName}">{ServiceSummary}</param>
    public {ControllerName}(
        AbsAsgardContext asgardContext,
        I{ServiceName} {serviceName})
        : base(asgardContext)
    {
        _{serviceName} = {serviceName};
    }

    /// <summary>
    /// {ServiceSummary}
    /// </summary>
    private readonly I{ServiceName} _{serviceName};
}
```

### 单条查询接口

```csharp
/// <summary>
/// {ActionSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <returns>操作结果</returns>
[HttpGet("{Route}")]
[ProducesResponseType(typeof(Response<{VoType}>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(Response<{VoType}>), StatusCodes.Status404NotFound)]
public async Task<ActionResult<Response<{VoType}>>> {ActionName}(
    [FromRoute] {ParameterType} {ParameterName})
{
    var dto = await _{serviceName}.{MethodName}({ParameterName});
    if (dto == null)
    {
        return NotFound<{VoType}>({NotFoundMessage});
    }

    var vo = _{mapperName}.Map<{VoType}>(dto);
    return Success(vo);
}
```

### 页码分页列表接口

```csharp
/// <summary>
/// {ActionSummary}
/// </summary>
/// <param name="page">页码</param>
/// <param name="size">每页大小</param>
/// <returns>分页数据列表</returns>
[HttpGet]
[ProducesResponseType(typeof(PageResponse<{VoType}>), StatusCodes.Status200OK)]
public async Task<ActionResult<PageResponse<{VoType}>>> {ActionName}(
    [FromQuery] int page = 1,
    [FromQuery] int size = 20)
{
    var (items, totalCount) = await _{serviceName}.{MethodName}(page, size);
    var vos = items.Select(_{mapperName}.Map<{VoType}>).ToList();
    return SuccessPage(vos, totalCount, page, size);
}
```

### 游标分页列表接口

```csharp
/// <summary>
/// {ActionSummary}
/// </summary>
/// <param name="cursor">游标</param>
/// <param name="size">每页大小</param>
/// <returns>游标分页数据列表</returns>
[HttpGet]
[ProducesResponseType(typeof(CursorResponse<{VoType}>), StatusCodes.Status200OK)]
public async Task<ActionResult<CursorResponse<{VoType}>>> {ActionName}(
    [FromQuery] string? cursor = null,
    [FromQuery] int size = 20)
{
    var (items, hasMore, nextCursor, lastId) = await _{serviceName}.{MethodName}(cursor, size);
    var vos = items.Select(_{mapperName}.Map<{VoType}>).ToList();
    return SuccessCursor(vos, hasMore, nextCursor, lastId);
}
```

### 启用异常处理

在 `Program.cs` 中间件管道**最开头**添加：

```csharp
app.UseAsgardExceptionHandler();
```

## 推荐做法

- 每个控制器只负责一个业务领域
- 为每个 Action 添加 `[ProducesResponseType]` 注释，便于 Swagger 生成文档
- 详情类 / 单资源接口优先让 `200` 与 `404` 共享同一个 `Response<TVo>` 标注，减少 Swagger 类型语义漂移
- 通过 `AsgardContext` 获取当前用户、租户等上下文信息
- 保持 Action 简洁，只做参数编排和结果返回
- 控制器文件放在 `Controllers/`，不要另起结构
- 需要文档时，在项目根目录 `app.yaml` 中开启 `host.swagger.enabled: true`
- 所有实现继续遵守 `$asgard-dotnet-10-csharp-14`
- 审查 Controller 时，优先检查返回类型是否仍然是 `Response` / `PageResponse` 家族

## 不要这样做

❌ 不要跳过分层边界，让 Controller 直接承担 Repository / Entity 访问

❌ 不要让 Service 直接返回给前端的响应壳模型，统一响应只属于 Controller 层

❌ 不要让 Controller 直接返回裸 `VO`、`DTO`、`string`、`bool`、`int`、`List<T>` 或 `IEnumerable<T>`

❌ 不要把业务逻辑直接写在 Action 里，保持职责分离

❌ 不要忽略分页响应的统一模型，所有列表接口都应该使用分页模型

❌ 不要在每个 Action 里重复编写大而全的 try/catch，交给全局异常处理

❌ 不要忘记注入 `AbsAsgardContext` 并传给基类构造函数

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `BaseController.cs` - 基础控制器实现
- `Response.cs` - 统一响应工厂
- `AsgardExceptionHandlerExtensions.cs` - 异常处理扩展

结构规范请参考 `$asgard-plugin-structure`。

代码范本请参考 `templates/` 目录，可直接替换占位符使用。
