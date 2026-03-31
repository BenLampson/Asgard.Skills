namespace Asgard.Core.Data;

/// <summary>
/// 仓储服务扩展方法，用于自动扫描和注册仓储类。
/// </summary>
/// <remarks>
/// <para>提供自动扫描程序集中带有 <see cref="RepositoryAttribute"/> 特性的仓储类。</para>
/// <para>支持将仓储类注册到依赖注入容器中。</para>
/// </remarks>
public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// 自动扫描并注册指定程序集中的仓储类。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="assemblies">要扫描的程序集。</param>
    /// <returns>服务集合实例，支持链式调用。</returns>
    /// <remarks>
    /// <para>此方法会扫描指定程序集中带有 <see cref="RepositoryAttribute"/> 特性的类。</para>
    /// <para>对于每个找到的仓储类，会将其注册为自身类型的服务。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 扫描当前程序集
    /// services.AddRepositories(typeof(Program).Assembly);
    /// 
    /// // 扫描多个程序集
    /// services.AddRepositories(
    ///     typeof(Program).Assembly,
    ///     typeof(UserRepository).Assembly
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddRepositories(this IServiceCollection services, params Assembly[] assemblies)
    {
        return AddRepositories(services, null, assemblies);
    }

    /// <summary>
    /// 自动扫描并注册指定程序集中的仓储类。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="logger">日志器实例，用于记录仓储注册过程。</param>
    /// <param name="assemblies">要扫描的程序集。</param>
    /// <returns>服务集合实例，支持链式调用。</returns>
    /// <remarks>
    /// <para>此方法会扫描指定程序集中带有 <see cref="RepositoryAttribute"/> 特性的类。</para>
    /// <para>对于每个找到的仓储类，会将其注册为自身类型的服务。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 扫描当前程序集并记录日志
    /// services.AddRepositories(logger, typeof(Program).Assembly);
    /// 
    /// // 扫描多个程序集并记录日志
    /// services.AddRepositories(
    ///     logger,
    ///     typeof(Program).Assembly,
    ///     typeof(UserRepository).Assembly
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddRepositories(this IServiceCollection services, ILogger? logger, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            throw new ArgumentException("至少需要指定一个程序集", nameof(assemblies));
        }

        var descriptors = new List<RepositoryDescriptor>();
        foreach (var assembly in assemblies)
        {
            descriptors.AddRange(RepositoryScanner.ScanRepositories(assembly));
        }

        foreach (var descriptor in descriptors)
        {
            // 注册仓储类本身
            _ = services.AddScoped(descriptor.ImplementationType, descriptor.ImplementationType);
            
            // 注册仓储类实现的所有接口
            var interfaces = descriptor.ImplementationType.GetInterfaces();
            foreach (var @interface in interfaces)
            {
                _ = services.AddScoped(@interface, descriptor.ImplementationType);
            }
        }

        logger?.LogInformation("已扫描并注册 {Count} 个仓储类", descriptors.Count);
        return services;
    }

    /// <summary>
    /// 自动扫描并注册指定类型所在程序集中的仓储类。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="type">用于获取程序集的类型。</param>
    /// <returns>服务集合实例，支持链式调用。</returns>
    /// <remarks>
    /// <para>此方法会扫描指定类型所在程序集中带有 <see cref="RepositoryAttribute"/> 特性的类。</para>
    /// <para>对于每个找到的仓储类，会将其注册为自身类型的服务。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 扫描 Program 类所在的程序集
    /// services.AddRepositories(typeof(Program));
    /// </code>
    /// </example>
    public static IServiceCollection AddRepositories(this IServiceCollection services, Type type)
    {
        return AddRepositories(services, null, type);
    }

    /// <summary>
    /// 自动扫描并注册指定类型所在程序集中的仓储类。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="logger">日志器实例，用于记录仓储注册过程。</param>
    /// <param name="type">用于获取程序集的类型。</param>
    /// <returns>服务集合实例，支持链式调用。</returns>
    /// <remarks>
    /// <para>此方法会扫描指定类型所在程序集中带有 <see cref="RepositoryAttribute"/> 特性的类。</para>
    /// <para>对于每个找到的仓储类，会将其注册为自身类型的服务。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 扫描 Program 类所在的程序集并记录日志
    /// services.AddRepositories(logger, typeof(Program));
    /// </code>
    /// </example>
    public static IServiceCollection AddRepositories(this IServiceCollection services, ILogger? logger, Type type)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(type);

        return services.AddRepositories(logger, type.Assembly);
    }
}
