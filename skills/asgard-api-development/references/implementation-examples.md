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

### 标准路由前缀

控制器级路由必须让最终访问地址落在 `https://xxx.com/api/xxxx` 形态下。推荐在 Controller 上声明基路由，再在 Action 上只写资源内的相对路径。

```csharp
[ApiController]
[Route("api/users")]
public class UsersController : BaseController
{
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<UserVo>>> GetAsync([FromRoute] long id)
    {
        // ...
    }
}
```

如果使用 `[controller]` token，也必须保留 `api` 前缀：

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
}
```

不要写成 `[Route("users")]`、`[Route("[controller]")]` 或只依赖 Action 上的裸路径，除非这是明确的框架端点、健康检查、Swagger 或 OIDC discovery/JWKS 等协议端点。

### 在 Controller 中获取当前用户信息

继承 `BaseController` 之后，框架不会自动给你一个单独的 `CurrentUserId` 属性，但基类已经提供了 `AsgardContext`，所以正确入口是：

```csharp
/// <summary>
/// 当前用户 ID。
/// </summary>
protected string? CurrentUserId => AsgardContext.IdentityContext?.UserInfo?.UserId;

/// <summary>
/// 当前用户主体标识。
/// </summary>
protected string CurrentSub => AsgardContext.IdentityContext?.UserInfo?.Sub ?? string.Empty;

/// <summary>
/// 当前租户 ID。
/// </summary>
protected string? CurrentTenantId => AsgardContext.IdentityContext?.UserInfo?.TenantId;
```

如果你的控制器需要频繁使用这些值，推荐在控制器内部定义成受保护属性，而不是每个 Action 都现写一遍长链式访问。

如果你的项目存在后台任务、匿名接口或系统初始化流程，请额外定义一套明确的审计回退策略，例如统一回退到固定系统标识，而不是在不同模块里各自兜底。

### 在新增/修改接口中写入审计字段

对于常见的增删改查，`CreateBy`、`UpdateBy`、租户归属等字段不要手填常量，也不要漏写，应该从身份上下文读取：

```csharp
/// <summary>
/// 创建数据。
/// </summary>
[HttpPost]
[Authorize]
[ProducesResponseType(typeof(Response<object>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<Response<object>>> CreateAsync([FromBody] Create{EntityName}Request request)
{
    var userId = AsgardContext.IdentityContext?.UserInfo?.UserId;
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Fail(StatusCodes.Status401Unauthorized, "当前登录信息无效，无法确定用户标识。");
    }

    await _{serviceName}.CreateAsync(new Create{EntityName}Input
    {
        Name = request.Name,
        CreateBy = userId,
        UpdateBy = userId,
        TenantId = AsgardContext.IdentityContext?.UserInfo?.TenantId
    });

    return Success("创建成功");
}
```

### 多租户接口安全示例（标准写法）

以下写法用于“租户用户只能访问自己租户”的默认规则：

```csharp
[HttpGet("{tenantId}/orders")]
[AsgardAuthAnyPermission("orders.read")]
public async Task<ActionResult<Response<List<OrderVo>>>> GetOrdersAsync([FromRoute] string tenantId)
{
    var effectiveTenantId = AsgardContext.IdentityContext?.UserInfo?.TenantId;
    if (string.IsNullOrWhiteSpace(effectiveTenantId))
    {
        return Fail<List<OrderVo>>(StatusCodes.Status401Unauthorized, "当前身份缺少租户信息。");
    }

    if (!string.Equals(tenantId, effectiveTenantId, StringComparison.OrdinalIgnoreCase))
    {
        return Fail<List<OrderVo>>(StatusCodes.Status403Forbidden, "禁止跨租户访问。");
    }

    var items = await _orderService.GetByTenantAsync(effectiveTenantId);
    return Success(items);
}
```

### 多租户接口安全示例（平台管理员例外）

如果业务允许“平台管理员跨租户”，必须把例外条件写成显式权限分支。以下只展示 Controller 入口校验；包含单实体共享缓存补丁时，`GetByTenantAsync` 的服务实现还必须授权目标租户并创建单租户范围，或使用经 `ICrossTenantScopeAuthorizer` 授权的跨租户范围。传入 tenantId 和布尔权限检查本身不会改变 FreeSql 范围，不能以禁用过滤替代。读取 [范围契约](../../asgard-database/references/shared-entity-cache-tenant-scopes.md)：

```csharp
[HttpGet("{tenantId}/orders")]
[AsgardAuthAnyPermission("orders.read", "platform.orders.read")]
public async Task<ActionResult<Response<List<OrderVo>>>> GetOrdersAsync([FromRoute] string tenantId)
{
    var userInfo = AsgardContext.IdentityContext?.UserInfo;
    if (string.IsNullOrWhiteSpace(userInfo?.TenantId))
    {
        return Fail<List<OrderVo>>(StatusCodes.Status401Unauthorized, "当前身份缺少租户信息。");
    }

    var canCrossTenant = userInfo.Permissions.Contains("platform.orders.read", StringComparer.OrdinalIgnoreCase);
    if (!canCrossTenant &&
        !string.Equals(tenantId, userInfo.TenantId, StringComparison.OrdinalIgnoreCase))
    {
        return Fail<List<OrderVo>>(StatusCodes.Status403Forbidden, "禁止跨租户访问。");
    }

    var items = await _orderService.GetByTenantAsync(tenantId);
    return Success(items);
}
```

### 更推荐的做法：在 Service 层统一取身份

如果审计字段在多个接口里都要用，推荐在 Service 层通过 `AbsAsgardContext` 统一读取，而不是散落在每个 Controller Action 中：

```csharp
public class {EntityName}Service(AbsAsgardContext asgardContext)
{
    private readonly AbsAsgardContext _asgardContext = asgardContext;

    private string GetRequiredUserId()
    {
        var userId = _asgardContext.IdentityContext?.UserInfo?.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("当前登录信息无效，无法确定用户标识。");
        }

        return userId;
    }
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
