namespace Asgard.Heimdall.JwtSigning.AspNetCore;

/// <summary>
/// 提供 Heimdall 轻量 JWT 签发服务注册扩展。
/// </summary>
public static class AsgardHeimdallJwtSigningServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Heimdall 轻量 JWT 签发服务。
    /// </summary>
    public static IServiceCollection AddAsgardHeimdallJwtSigning(
        this IServiceCollection services,
        Action<AsgardJwtSigningOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        _ = services
            .AddOptions<AsgardJwtSigningOptions>()
            .Configure(configure)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Issuer 不能为空。")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Audience 不能为空。")
            .Validate(options => !string.IsNullOrWhiteSpace(options.KeyId), "KeyId 不能为空。")
            .Validate(options => IsValidDiscoveryPathPrefix(options.DiscoveryPathPrefix), "DiscoveryPathPrefix 必须为空或以 / 开头。")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RsaPrivateKeyPem) || !string.IsNullOrWhiteSpace(options.SymmetricSecurityKey), "必须提供 RsaPrivateKeyPem 或 SymmetricSecurityKey。")
            .ValidateOnStart();

        _ = services.AddSingleton<IAsgardJwtIssuer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AsgardJwtSigningOptions>>().Value;
            return new AsgardJwtIssuer(options);
        });

        return services;
    }

    private static bool IsValidDiscoveryPathPrefix(string? pathPrefix)
        => string.IsNullOrWhiteSpace(pathPrefix) || pathPrefix.StartsWith("/", StringComparison.Ordinal);
}
