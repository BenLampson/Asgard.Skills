namespace Asgard.Abstractions.Identity;

public static class AsgardClaimTypes
{
    public const string Sub = "sub";

    public const string UserId = "user_id";

    public const string TenantId = "tenant_id";

    public const string ClientId = "client_id";

    public const string Roles = "roles";

    public const string Permissions = "permissions";

    public const string Scope = "scope";

    public const string UserMetadatas = "userMetadatas";

    public const string TenantMetadata = "tenantMetadata";

    public const string TokenType = "token_type";

    public static IReadOnlyList<string> ClientIdAliases { get; } =
    [
        ClientId,
        "azp",
        "appid",
        "app_id"
    ];

    public static IReadOnlyList<string> TokenTypeAliases { get; } =
    [
        TokenType,
        "tokenType",
        "asgard_token_type",
        "asgard:token_type",
        "cty",
        "typ"
    ];
}
