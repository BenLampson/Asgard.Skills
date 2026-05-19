namespace Asgard.Heimdall.JwtSigning;

/// <summary>
/// 表示 Asgard 轻量 JWT 签发配置。
/// </summary>
public sealed class AsgardJwtSigningOptions
{
    /// <summary>
    /// 获取或设置签发者。
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置默认受众。
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置密钥标识。
    /// </summary>
    public string KeyId { get; set; } = "asgard-heimdall-jwt-signing";

    /// <summary>
    /// 获取或设置 PEM 格式 RSA 私钥。
    /// </summary>
    public string? RsaPrivateKeyPem { get; set; }

    /// <summary>
    /// 获取或设置 PEM 格式 RSA 公钥。
    /// </summary>
    public string? RsaPublicKeyPem { get; set; }

    /// <summary>
    /// 获取或设置对称密钥，适用于 HS256。
    /// </summary>
    public string? SymmetricSecurityKey { get; set; }

    /// <summary>
    /// 获取或设置签名算法。
    /// </summary>
    public string Algorithm { get; set; } = AsgardJwtConstants.DefaultAlgorithm;

    /// <summary>
    /// 获取或设置访问令牌默认生命周期。
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 获取或设置默认令牌主体类型。
    /// </summary>
    public string DefaultTokenType { get; set; } = AsgardJwtConstants.UserLoginTokenType;
}
