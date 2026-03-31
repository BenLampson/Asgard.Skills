namespace Asgard.Abstractions.AspNetCore.Model;

/// <summary>
/// 通用响应工厂。
/// 用于统一创建成功、失败、分页和游标分页响应对象。
/// </summary>
public static partial class Response
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
}
