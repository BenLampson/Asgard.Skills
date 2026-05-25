namespace Asgard.Abstractions.AspNetCore.Host;

/// <summary>
/// 宿主认证配置。
/// </summary>
/// <remarks>
/// 当前宿主仅支持 Bearer JWT 模式，认证实现基于 OIDC discovery 与 JWKS 自发现。
/// </remarks>
public class AuthOptions
{
    /// <summary>
    /// 是否启用宿主内置 JWT 认证链路。
    /// </summary>
    /// <remarks>
    /// 该开关只控制宿主默认提供的 Bearer JWT 注册与 <c>UseAuthentication()</c> 接线，
    /// 以及 Swagger 中的 Bearer 展示。
    /// Asgard 自身的授权策略、多租户能力和授权中间件不会因该开关关闭而失效。
    /// </remarks>
    /// <value><see langword="true"/> 表示启用宿主内置 JWT；否则关闭该默认认证实现。</value>
    [ConfigPath("host.auth.enabled", DefaultValue = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// JWT Bearer 认证运行时配置。
    /// </summary>
    /// <remarks>
    /// 包含 issuer 模板、audience、HTTPS 约束和 discovery/JWKS 缓存策略。
    /// </remarks>
    /// <value>JWT Bearer 认证配置。</value>
    [ConfigPath("host.auth.jwt")]
    public JwtOptions Jwt { get; set; } = new();

    /// <summary>
    /// 校验认证配置是否有效。
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    public void Validate()
    {
        // 关闭宿主内置 JWT 时，允许用户完全不提供 jwt 子配置，
        // 以便改由插件、网关或外部中间件接管认证主体构建。
        if (!Enabled)
        {
            return;
        }

        if (Jwt is null)
        {
            throw new InvalidOperationException("JWT 认证配置不能为空。");
        }

        Jwt.Validate();
    }
}

