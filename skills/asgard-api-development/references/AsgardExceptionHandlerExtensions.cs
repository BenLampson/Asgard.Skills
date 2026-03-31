namespace Asgard.Abstractions.AspNetCore.Extensions;

/// <summary>
/// Asgard异常处理中间件扩展方法
/// </summary>
public static class AsgardExceptionHandlerExtensions
{
    /// <summary>
    /// 使用Asgard标准异常处理中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    /// <remarks>
    /// <para>此方法应放在中间件管道的最开始位置</para>
    /// <para>自动捕获所有未处理异常并返回项目标准格式的响应</para>
    /// </remarks>
    public static IApplicationBuilder UseAsgardExceptionHandler(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        
        return app.UseMiddleware<AsgardExceptionHandlerMiddleware>();
    }
}
