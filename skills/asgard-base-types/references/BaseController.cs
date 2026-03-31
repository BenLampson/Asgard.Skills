namespace Asgard.Abstractions.AspNetCore.Controller;

/// <summary>
/// 基础控制器。
/// 提供通用的控制器功能和依赖注入支持。
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Asgard 框架统一上下文。
    /// </summary>
    protected readonly AbsAsgardContext AsgardContext;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="asgardContext">Asgard 上下文实例。</param>
    /// <exception cref="ArgumentNullException">当 asgardContext 为 null 时抛出。</exception>
    protected BaseController(AbsAsgardContext asgardContext)
    {
        ArgumentNullException.ThrowIfNull(asgardContext);
        AsgardContext = asgardContext;
    }

    #region Success Responses

    /// <summary>
    /// 返回成功响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="data">响应数据。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>成功响应结果。</returns>
    protected ActionResult<Model.Response<T>> Success<T>(T data, string message = "操作成功")
    {
        return Ok(Model.Response.Success(data, message));
    }

    /// <summary>
    /// 返回成功响应（无数据）。
    /// </summary>
    /// <param name="message">响应消息。</param>
    /// <returns>成功响应结果。</returns>
    protected ActionResult<Model.Response<object>> Success(string message = "操作成功")
    {
        return Ok(Model.Response.Success(message));
    }

    /// <summary>
    /// 返回页码分页成功响应。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前页数据。</param>
    /// <param name="totalCount">全量数据总条数。</param>
    /// <param name="page">当前页码。</param>
    /// <param name="size">每页大小。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>分页成功响应结果。</returns>
    protected ActionResult<Model.PageResponse<TItem>> SuccessPage<TItem>(
        IReadOnlyList<TItem> data,
        long totalCount,
        int page,
        int size,
        string message = "操作成功")
    {
        return Ok(Model.Response.SuccessPage(data, totalCount, page, size, message));
    }

    /// <summary>
    /// 返回页码分页成功响应（可枚举版本）。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前页数据。</param>
    /// <param name="totalCount">全量数据总条数。</param>
    /// <param name="page">当前页码。</param>
    /// <param name="size">每页大小。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>分页成功响应结果。</returns>
    protected ActionResult<Model.PageResponse<TItem>> SuccessPage<TItem>(
        IEnumerable<TItem> data,
        long totalCount,
        int page,
        int size,
        string message = "操作成功")
    {
        return Ok(Model.Response.SuccessPage(data, totalCount, page, size, message));
    }

    /// <summary>
    /// 返回游标分页成功响应。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前批次数据。</param>
    /// <param name="hasMore">是否还有更多数据。</param>
    /// <param name="nextCursor">下一批数据的游标。</param>
    /// <param name="lastId">当前批次最后一条数据的标识。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>游标分页成功响应结果。</returns>
    protected ActionResult<Model.CursorResponse<TItem>> SuccessCursor<TItem>(
        IReadOnlyList<TItem> data,
        bool hasMore,
        string? nextCursor = null,
        string? lastId = null,
        string message = "操作成功")
    {
        return Ok(Model.Response.SuccessCursor(data, hasMore, nextCursor, lastId, message));
    }

    /// <summary>
    /// 返回游标分页成功响应（可枚举版本）。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前批次数据。</param>
    /// <param name="hasMore">是否还有更多数据。</param>
    /// <param name="nextCursor">下一批数据的游标。</param>
    /// <param name="lastId">当前批次最后一条数据的标识。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>游标分页成功响应结果。</returns>
    protected ActionResult<Model.CursorResponse<TItem>> SuccessCursor<TItem>(
        IEnumerable<TItem> data,
        bool hasMore,
        string? nextCursor = null,
        string? lastId = null,
        string message = "操作成功")
    {
        return Ok(Model.Response.SuccessCursor(data, hasMore, nextCursor, lastId, message));
    }

    #endregion

    #region Failure Responses

    /// <summary>
    /// 返回失败响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="code">错误状态码。</param>
    /// <param name="message">错误消息。</param>
    /// <returns>失败响应结果。</returns>
    protected ActionResult<Model.Response<T>> Fail<T>(int code, string message)
    {
        return Ok(Model.Response.Fail<T>(code, message));
    }

    /// <summary>
    /// 返回参数错误响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="message">错误消息。</param>
    /// <returns>参数错误响应结果。</returns>
    protected ActionResult<Model.Response<T>> BadRequest<T>(string message = "参数错误")
    {
        return Ok(Model.Response.BadRequest<T>(message));
    }

    /// <summary>
    /// 返回未找到响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="message">错误消息。</param>
    /// <returns>未找到响应结果。</returns>
    protected ActionResult<Model.Response<T>> NotFound<T>(string message = "Resource not found")
    {
        return Ok(Model.Response.NotFound<T>(message));
    }

    /// <summary>
    /// 返回服务器错误响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="message">错误消息。</param>
    /// <returns>服务器错误响应结果。</returns>
    protected ActionResult<Model.Response<T>> ServerError<T>(string message = "Internal server error")
    {
        return Ok(Model.Response.ServerError<T>(message));
    }

    #endregion
}
