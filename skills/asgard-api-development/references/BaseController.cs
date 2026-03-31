namespace Asgard.Abstractions.AspNetCore.Controller;

/// <summary>
/// 基础控制器
/// 提供通用的控制器功能和依赖注入支持
/// </summary>
[ApiController] 
public class BaseController : ControllerBase
{
    /// <summary>
    /// Asgard 上下文
    /// </summary>
    protected readonly AbsAsgardContext AsgardContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asgardContext">Asgard 上下文</param>
    /// <exception cref="ArgumentNullException">当 asgardContext 为 null 时抛出</exception>
    public BaseController(AbsAsgardContext asgardContext)
    {
        ArgumentNullException.ThrowIfNull(asgardContext);
        AsgardContext = asgardContext;
    }

    /// <summary>
    /// 返回成功响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="data">响应数据</param>
    /// <param name="message">响应消息</param>
    /// <returns>成功响应</returns>
    protected ActionResult<Model.Response<T>> Success<T>(T data, string message = "操作成功")
    {
        return Ok(Model.Response.Success(data, message));
    }

    /// <summary>
    /// 返回成功响应（无数据）
    /// </summary>
    /// <param name="message">响应消息</param>
    /// <returns>成功响应</returns>
    protected ActionResult<Model.Response<object>> Success(string message = "操作成功")
    {
        return Ok(Model.Response.Success(message));
    }

    /// <summary>
    /// 返回页码分页成功响应。
    /// 用于需要总量、当前页和页大小信息的标准分页接口。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前页数据。</param>
    /// <param name="totalCount">全量数据总条数。</param>
    /// <param name="page">当前页码。</param>
    /// <param name="size">当前每页大小。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>页码分页成功响应。</returns>
    protected ActionResult<PageResponse<TItem>> SuccessPage<TItem>(
        IReadOnlyList<TItem> data,
        long totalCount,
        int page,
        int size,
        string message = "操作成功")
    {
        return Ok(Model.Response.SuccessPage(data, totalCount, page, size, message));
    }

    /// <summary>
    /// 返回游标分页成功响应。
    /// 用于瀑布流、无限滚动和按主键递进的查询接口。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前批次数据。</param>
    /// <param name="hasMore">是否仍有更多数据。</param>
    /// <param name="nextCursor">下一批数据游标。</param>
    /// <param name="lastId">当前批次最后一条数据标识。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>游标分页成功响应。</returns>
    protected ActionResult<CursorResponse<TItem>> SuccessCursor<TItem>(
        IReadOnlyList<TItem> data,
        bool hasMore,
        string? nextCursor = null,
        string? lastId = null,
        string message = "操作成功")
    {
        return Ok(Model.Response.SuccessCursor(data, hasMore, nextCursor, lastId, message));
    }

    /// <summary>
    /// 返回失败响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="code">状态码</param>
    /// <param name="message">错误消息</param>
    /// <returns>失败响应</returns>
    protected ActionResult<Model.Response<T>> Fail<T>(int code, string message)
    {
        return StatusCode(code, Model.Response.Fail<T>(code, message));
    }

    /// <summary>
    /// 返回失败响应（无数据）
    /// </summary>
    /// <param name="code">状态码</param>
    /// <param name="message">错误消息</param>
    /// <returns>失败响应</returns>
    protected ActionResult<Model.Response<object>> Fail(int code, string message)
    {
        return StatusCode(code, Model.Response.Fail(code, message));
    }

    /// <summary>
    /// 返回参数错误响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>参数错误响应</returns>
    protected ActionResult<Model.Response<T>> BadRequest<T>(string message = "参数错误")
    {
        return base.BadRequest(Model.Response.BadRequest<T>(message));
    }

    /// <summary>
    /// 返回未找到响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>未找到响应</returns>
    protected ActionResult<Model.Response<T>> NotFound<T>(string message = "Resource not found")
    {
        return base.NotFound(Model.Response.NotFound<T>(message));
    }

    /// <summary>
    /// 返回服务器错误响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>服务器错误响应</returns>
    protected ActionResult<Model.Response<T>> ServerError<T>(string message = "Internal server error")
    {
        return StatusCode(500, Model.Response.ServerError<T>(message));
    }
}
