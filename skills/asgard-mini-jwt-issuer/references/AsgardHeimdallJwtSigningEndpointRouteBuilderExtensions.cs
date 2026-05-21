namespace Asgard.Heimdall.JwtSigning.AspNetCore;

/// <summary>
/// 提供 Heimdall 轻量 JWT discovery 端点映射扩展。
/// </summary>
public static class AsgardHeimdallJwtSigningEndpointRouteBuilderExtensions
{
    private static readonly string[] _claimsSupported =
    [
        AsgardClaimTypes.Sub,
        AsgardClaimTypes.UserId,
        AsgardClaimTypes.TenantId,
        AsgardClaimTypes.ClientId,
        AsgardClaimTypes.Roles,
        AsgardClaimTypes.Permissions,
        AsgardClaimTypes.Scope,
        AsgardClaimTypes.UserMetadatas,
        AsgardClaimTypes.TenantMetadata,
        AsgardClaimTypes.TokenType
    ];

    /// <summary>
    /// 映射 Heimdall 轻量 JWT discovery 与 JWKS 端点。
    /// </summary>
    public static IEndpointRouteBuilder MapAsgardHeimdallJwtSigningDiscovery(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AsgardJwtSigningOptions>>().Value;
        var pathPrefix = NormalizePathPrefix(options.DiscoveryPathPrefix);
        var openIdConfigurationPath = $"{pathPrefix}/.well-known/openid-configuration";
        var jwksPath = $"{pathPrefix}/.well-known/jwks.json";

        _ = endpoints.MapGet(openIdConfigurationPath, (IOptions<AsgardJwtSigningOptions> options) =>
        {
            var issuer = options.Value.Issuer.TrimEnd('/');
            var jwksUri = BuildJwksUri(options.Value, issuer);
            return Results.Json(new Dictionary<string, object>
            {
                ["issuer"] = issuer,
                ["jwks_uri"] = jwksUri,
                ["claims_supported"] = _claimsSupported,
                ["id_token_signing_alg_values_supported"] = new[] { options.Value.Algorithm },
                ["response_types_supported"] = Array.Empty<string>(),
                ["subject_types_supported"] = new[] { "public" }
            });
        });

        _ = endpoints.MapGet(jwksPath, (IAsgardJwtIssuer issuer) => Results.Json(issuer.CreateJwksDocument()));

        return endpoints;
    }

    private static string NormalizePathPrefix(string? pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix) || pathPrefix == "/")
        {
            return string.Empty;
        }

        return pathPrefix.TrimEnd('/');
    }

    private static string BuildJwksUri(AsgardJwtSigningOptions options, string issuer)
        => string.IsNullOrWhiteSpace(options.JwksUriOverride)
            ? $"{issuer}/.well-known/jwks.json"
            : options.JwksUriOverride.Trim();
}
