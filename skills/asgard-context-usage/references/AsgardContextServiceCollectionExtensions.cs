namespace Asgard.Core.AsgardContextModule;

/// <summary>
/// Asgard 上下文服务集合扩展方法。
/// </summary>
/// <remarks>
/// <para>
/// 提供统一的扩展方法用于注入 <see cref="AsgardContext"/> 服务。
/// </para>
/// <para>
/// <b>使用示例：</b>
/// <code>
/// // 在 ConfigureServices 中注入
/// services.AddAsgardContext();
/// 
/// // 在服务中使用
/// public class MyService
/// {
///     private readonly AbsAsgardContext _context;
///     public MyService(AbsAsgardContext context)
///     {
///         _context = context;
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public static class AsgardContextServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Asgard 上下文服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <remarks>
    /// <para>
    /// 此方法将 <see cref="AsgardContext"/> 注册为 <see cref="AbsAsgardContext"/> 的 Scoped 生命周期实现。
    /// </para>
    /// <para>
    /// <b>生命周期说明：</b>
    /// <list type="bullet">
    ///   <item><description>Scoped：每次请求创建新实例，适合租户隔离等场景</description></item>
    ///   <item><description>依赖注入会自动注入已注册的模块服务</description></item>
    ///   <item><description>未注册的服务属性为 null，用户可优雅降级</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>注册顺序：</b>
    /// 建议在其他模块服务注册完成后再调用此方法，确保所有需要的服务都能被正确注入。
    /// <code>
    /// // 推荐顺序
    /// services.AddMultiLevelCache();
    /// services.AddMessageQueue();
    /// services.AddAsgardContext(); // 最后注入
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // 注册其他模块
    /// builder.Services.AddMultiLevelCache(builder.Configuration);
    /// builder.Services.AddMessageQueue(builder.Configuration);
    /// builder.Services.AddJobScheduler(builder.Configuration);
    /// 
    /// // 注册 Asgard 上下文
    /// builder.Services.AddAsgardContext();
    /// 
    /// var app = builder.Build();
    /// </code>
    /// </example>
    public static IServiceCollection AddAsgardContext(this IServiceCollection services)
    {
        _=services.AddScoped<IAsgardRepositoryContext, AsgardRepositoryContext>();
        _=services.AddScoped<AbsAsgardContext, AsgardContext>();
        return services;
    }
}
