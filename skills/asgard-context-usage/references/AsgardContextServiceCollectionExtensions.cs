namespace Asgard.Core.AsgardContextModule;

/// <summary>注册作用域级业务上下文与宿主级能力诊断记录。</summary>
public static class AsgardContextServiceCollectionExtensions
{
    /// <summary>可选模块按实际注册和配置开关按需解析，未装配时返回 Disabled 能力。</summary>
    public static IServiceCollection AddAsgardContext(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddScoped<IAsgardRepositoryContext, AsgardRepositoryContext>();
        _ = services.AddScoped<AbsAsgardContext>(provider => new AsgardContext(provider, provider.GetRequiredService<FeatureHistory>()));
        FeatureRegistrations.Register(services);
        return services;
    }

    /// <summary>注册带状态及公开配置的延迟工厂，Context 读取这些信息时不会构造服务。</summary>
    public static IServiceCollection AddAsgardFeature<T>(this IServiceCollection services, Func<IServiceProvider, T> factory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped, AsgardFeatureStatus status = AsgardFeatureStatus.Enabled,
        IReadOnlyDictionary<string, string>? configuration = null) where T : class, IAsgardFeature
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        var declaration = new DeclaredFeatureFactory(factory, status, configuration);
        services.Add(new ServiceDescriptor(typeof(T), declaration.Create, lifetime));
        FeatureRegistrations.Register(services);
        return services;
    }

    /// <summary>显式注册已经构造的能力并发布其状态及公开配置。</summary>
    public static IServiceCollection AddAsgardFeature<T>(this IServiceCollection services, T implementation) where T : class, IAsgardFeature
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddSingleton(implementation);
        FeatureRegistrations.Register(services);
        return services;
    }

    /// <summary>使用当前服务集合的独立快照构建容器，适用于复用同一集合或需要读取后置注册声明的自定义宿主。</summary>
    public static ServiceProvider BuildAsgardServiceProvider(this IServiceCollection services, ServiceProviderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        IServiceCollection snapshot = new ServiceCollection();
        foreach (var descriptor in services) snapshot.Add(descriptor);
        FeatureRegistrations.Register(snapshot, final: true);
        return snapshot.BuildServiceProvider(options ?? new ServiceProviderOptions());
    }
}
