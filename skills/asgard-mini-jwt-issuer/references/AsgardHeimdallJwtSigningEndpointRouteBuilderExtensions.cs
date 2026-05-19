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

        _ = endpoints.MapGet("/.well-known/openid-configuration", (HttpContext httpContext, IOptions<AsgardJwtSigningOptions> options) =>
        {
            var issuer = options.Value.Issuer.TrimEnd('/');
            var jwksUri = BuildAbsoluteUri(httpContext, "/.well-known/jwks.json");
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

        _ = endpoints.MapGet("/.well-known/jwks.json", (IAsgardJwtIssuer issuer) => Results.Json(issuer.CreateJwksDocument()));

        return endpoints;
    }

    private static string BuildAbsoluteUri(HttpContext httpContext, string path)
    {
        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}{path}";
    }
}
