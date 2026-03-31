namespace Asgard.Abstractions.AspNetCore.Model;

/// <summary>
/// 通用响应工厂。
/// 用于统一创建成功、失败、分页和游标分页响应对象。
/// </summary>
public static class Response
{
    /// <summary>
    /// 创建成功响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="data">响应数据。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>成功响应。</returns>
    public static Response<T> Success<T>(T data, string message = "操作成功")
    {
        return new Response<T>(200, message, data);
    }

    /// <summary>
    /// 创建成功响应。
    /// 用于无业务数据返回但需要保留统一响应结构的场景。
    /// </summary>
    /// <param name="message">响应消息。</param>
    /// <returns>成功响应。</returns>
    public static Response<object> Success(string message = "操作成功")
    {
        return new Response<object>(200, message, null);
    }

    /// <summary>
    /// 创建页码分页成功响应。
    /// 用于需要总量、页码和页大小信息的标准分页接口。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前页数据。</param>
    /// <param name="totalCount">全量数据总条数。</param>
    /// <param name="page">当前页码。</param>
    /// <param name="size">当前每页大小。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>页码分页响应。</returns>
    public static PageResponse<TItem> SuccessPage<TItem>(
        IReadOnlyList<TItem> data,
        long totalCount,
        int page,
        int size,
        string message = "操作成功")
    {
        return new PageResponse<TItem>(data, totalCount, page, size, message);
    }

    /// <summary>
    /// 创建页码分页成功响应。
    /// 当调用方只有可枚举数据时，会先物化为数组，避免重复枚举。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前页数据。</param>
    /// <param name="totalCount">全量数据总条数。</param>
    /// <param name="page">当前页码。</param>
    /// <param name="size">当前每页大小。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>页码分页响应。</returns>
    public static PageResponse<TItem> SuccessPage<TItem>(
        IEnumerable<TItem> data,
        long totalCount,
        int page,
        int size,
        string message = "操作成功")
    {
        ArgumentNullException.ThrowIfNull(data);

        var items = data as IReadOnlyList<TItem> ?? data.ToArray();

        return SuccessPage(items, totalCount, page, size, message);
    }

    /// <summary>
    /// 创建游标分页成功响应。
    /// 用于瀑布流、无限滚动和基于主键递进的接口返回。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前批次数据。</param>
    /// <param name="hasMore">是否仍有更多数据。</param>
    /// <param name="nextCursor">下一批数据的游标。</param>
    /// <param name="lastId">当前批次最后一条数据的标识。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>游标分页响应。</returns>
    public static CursorResponse<TItem> SuccessCursor<TItem>(
        IReadOnlyList<TItem> data,
        bool hasMore,
        string? nextCursor = null,
        string? lastId = null,
        string message = "操作成功")
    {
        return new CursorResponse<TItem>(data, hasMore, nextCursor, lastId, message);
    }

    /// <summary>
    /// 创建游标分页成功响应。
    /// 当调用方只有可枚举数据时，会先物化为数组，避免重复枚举。
    /// </summary>
    /// <typeparam name="TItem">列表项类型。</typeparam>
    /// <param name="data">当前批次数据。</param>
    /// <param name="hasMore">是否仍有更多数据。</param>
    /// <param name="nextCursor">下一批数据的游标。</param>
    /// <param name="lastId">当前批次最后一条数据的标识。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>游标分页响应。</returns>
    public static CursorResponse<TItem> SuccessCursor<TItem>(
        IEnumerable<TItem> data,
        bool hasMore,
        string? nextCursor = null,
        string? lastId = null,
        string message = "操作成功")
    {
        ArgumentNullException.ThrowIfNull(data);

        var items = data as IReadOnlyList<TItem> ?? data.ToArray();

        return SuccessCursor(items, hasMore, nextCursor, lastId, message);
    }

    /// <summary>
    /// 创建失败响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="code">状态码。</param>
    /// <param name="message">错误消息。</param>
    /// <returns>失败响应。</returns>
    public static Response<T> Fail<T>(int code, string message)
    {
        return new Response<T>(code, message);
    }

    /// <summary>
    /// 创建失败响应。
    /// 用于无数据失败返回场景。
    /// </summary>
    /// <param name="code">状态码。</param>
    /// <param name="message">错误消息。</param>
    /// <returns>失败响应。</returns>
    public static Response<object> Fail(int code, string message)
    {
        return new Response<object>(code, message);
    }

    /// <summary>
    /// 创建参数错误响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="message">错误消息。</param>
    /// <returns>参数错误响应。</returns>
    public static Response<T> BadRequest<T>(string message = "参数错误")
    {
        return new Response<T>(400, message);
    }

    /// <summary>
    /// 创建未找到响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="message">错误消息。</param>
    /// <returns>未找到响应。</returns>
    public static Response<T> NotFound<T>(string message = "Resource not found")
    {
        return new Response<T>(404, message);
    }

    /// <summary>
    /// 创建服务器错误响应。
    /// </summary>
    /// <typeparam name="T">响应数据类型。</typeparam>
    /// <param name="message">错误消息。</param>
    /// <returns>服务器错误响应。</returns>
    public static Response<T> ServerError<T>(string message = "Internal server error")
    {
        return new Response<T>(500, message);
    }
}
