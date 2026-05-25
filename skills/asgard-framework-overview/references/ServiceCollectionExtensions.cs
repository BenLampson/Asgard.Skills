namespace Asgard.AspNetCore.Core;

/// <summary>
/// 注册 Asgard 在 ASP.NET Core 场景下所需的核心服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 为 ASP.NET Core 主机注册环境身份、授权与租户相关服务。
    /// </summary>
    /// <param name="services">待扩展的服务集合。</param>
    /// <returns>扩展后的服务集合。</returns>
    public static IServiceCollection AddAsgardAspNetCore(this IServiceCollection services)
    {
        return services
            .AddSingleton<AsgardIdentityContextAccessor>()
            .AddSingleton<IAsgardIdentityContext>(provider => provider.GetRequiredService<AsgardIdentityContextAccessor>())
            .AddSingleton<IAsgardIdentityContextWriter>(provider => provider.GetRequiredService<AsgardIdentityContextAccessor>())
            .AddSingleton<AsgardTraceContextAccessor>()
            .AddSingleton<IAsgardTraceContext>(provider => provider.GetRequiredService<AsgardTraceContextAccessor>())
            .AddSingleton<IAsgardTraceScopeFactory>(provider => provider.GetRequiredService<AsgardTraceContextAccessor>())
            .AddSingleton<AsgardTraceActionFilter>()
            .AddAsgardTraceStoreFallback()
            .AddSingleton<IAsgardIdentityContextResolver, DefaultAsgardIdentityContextResolver>()
            .AddSingleton<AsgardTokenConventionValidator>()
            .AddSingleton<AsgardAuthExpressionParser>()
            .AddSingleton<AsgardAuthMetadataExpressionBuilder>()
            .AddSingleton<AsgardAuthFunctionDispatcher>()
            .AddSingleton<AsgardAuthEvaluator>()
            .AddSingleton<IAuthorizationHandler, AsgardAuthHandler>()
            .AddAsgardAuthInfrastructure()
            .AddAuthorizationBuilder()
            .AddPolicy(
                AsgardAuthConstants.PolicyName,
                policy => _ = policy.RequireAuthenticatedUser().AddRequirements(new AsgardAuthRequirement()))
            .Services
            .AddAsgardTenant();
    }

    private static IServiceCollection AddAsgardTraceStoreFallback(this IServiceCollection services)
    {
        services.TryAddSingleton<IAsgardTraceStore>(NullAsgardTraceStore.Instance);
        return services;
    }

    private static IServiceCollection AddAsgardAuthInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IAsgardAuthDataResolver, NullAsgardAuthDataResolver>();
        services.TryAddSingleton<IAsgardAuthFunctionRegistry, DefaultAsgardAuthFunctionRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAsgardAuthFunction, AsgardAuthContainsFunction>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAsgardAuthFunction, AsgardAuthStartsWithFunction>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAsgardAuthFunction, AsgardAuthExistsFunction>());
        return services;
    }
}
