namespace Asgard.Abstractions.AspNetCore.Host;

/// <summary>
/// ASP.NET Core 主机配置。
/// </summary>
/// <remarks>
/// 实现 <see cref="ISystemConfig"/> 接口，提供 Asgard 框架的主机级配置，
/// 包括 Kestrel 服务器、CORS、认证、Swagger、限流和健康检查等配置。
/// </remarks>
/// <example>
/// <code>
/// host:
///   application:
///     name: "Asgard.Yggdrasil"
///     version: "1.0.0"
///     environment: "Development"
///   kestrel:
///     endpoints:
///       http:
///         url: "http://localhost:5000"
///   cors:
///     enabled: true
///     defaultPolicy:
///       allowAnyOrigin: true
///       allowAnyMethod: true
///       allowAnyHeader: true
///   auth:
///     enabled: true
///   swagger:
///     enabled: true
///     title: "Asgard API"
///     version: "v1"
/// </code>
/// </example>
public class HostConfig : ISystemConfig
{
    /// <summary>
    /// 应用程序配置。
    /// </summary>
    /// <remarks>
    /// 必填配置，包含应用名称、版本和环境信息，用于 Swagger 文档显示等。
    /// </remarks>
    /// <value>应用程序配置项</value>
    [ConfigPath("host.application")]
    public ApplicationOptions Application { get; set; } = new();

    /// <summary>
    /// Kestrel 服务器配置。
    /// </summary>
    /// <remarks>
    /// 必填配置，Kestrel 是 ASP.NET Core 的 Web 服务器，支持 HTTP 和 HTTPS 端点配置。
    /// </remarks>
    /// <value>Kestrel 服务器配置项</value>
    [ConfigPath("host.kestrel")]
    public KestrelOptions Kestrel { get; set; } = new();

    /// <summary>
    /// CORS 跨域配置。
    /// </summary>
    /// <remarks>
    /// 可选配置，用于控制 API 的跨域访问策略，默认不启用。
    /// </remarks>
    /// <value>CORS 配置项</value>
    [ConfigPath("host.cors")]
    public CorsOptions? Cors { get; set; }

    /// <summary>
    /// 认证配置。
    /// </summary>
    /// <remarks>
    /// 可选配置，支持 JWT 和 Cookie 等认证方式，默认不启用。
    /// </remarks>
    /// <value>认证配置项</value>
    [ConfigPath("host.auth")]
    public AuthOptions? Auth { get; set; }

    /// <summary>
    /// Swagger/OpenAPI 文档配置。
    /// </summary>
    /// <remarks>
    /// 可选配置，用于生成和展示 API 的 Swagger UI 文档，默认不启用。
    /// </remarks>
    /// <value>Swagger 配置项</value>
    [ConfigPath("host.swagger")]
    public SwaggerOptions? Swagger { get; set; }

    /// <summary>
    /// TypeScript 客户端导出配置。
    /// </summary>
    [ConfigPath("host.tsGen")]
    public TsGenHostOptions? TsGen { get; set; }

    /// <summary>
    /// 限流配置。
    /// </summary>
    /// <remarks>
    /// 可选配置，用于保护 API 免受滥用，默认不启用。
    /// </remarks>
    /// <value>限流配置项</value>
    [ConfigPath("host.rateLimiting")]
    public RateLimitingOptions? RateLimiting { get; set; }

    /// <summary>
    /// 健康检查配置。
    /// </summary>
    /// <remarks>
    /// 可选配置，用于监控应用程序健康状态，默认不启用。
    /// </remarks>
    /// <value>健康检查配置项</value>
    [ConfigPath("host.healthCheck")]
    public HealthCheckOptions? HealthCheck { get; set; }

    /// <summary>
    /// 静态文件配置。
    /// </summary>
    /// <remarks>
    /// 控制是否暴露静态资源目录，以及自定义静态资源根目录和访问前缀。
    /// 默认启用并映射应用根目录下的 <c>wwwroot</c>。
    /// </remarks>
    /// <value>静态文件配置项。</value>
    [ConfigPath("host.staticFiles")]
    public StaticFileHostOptions StaticFiles { get; set; } = new();

    /// <summary>
    /// 校验配置项有效性。
    /// </summary>
    /// <remarks>
    /// 只有 Application 和 Kestrel 配置是必填的，其他配置为可选。
    /// 非必填配置只有在不为 null 时才进行验证。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当必填配置为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Application, nameof(Application));
        ArgumentNullException.ThrowIfNull(Kestrel, nameof(Kestrel));

        Application.Validate();
        Kestrel.Validate();

        Cors?.Validate();
        Auth?.Validate();
        Swagger?.Validate();
        TsGen?.Validate();
        RateLimiting?.Validate();
        HealthCheck?.Validate();
        StaticFiles.Validate();
    }
}
