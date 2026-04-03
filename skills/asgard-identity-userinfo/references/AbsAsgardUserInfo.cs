namespace Asgard.Abstractions.Identity;

/// <summary>
/// 框架级别的抽象用户信息基类，支持从 Claim[] 初始化和转换为 Claim[]。
/// </summary>
public abstract class AbsAsgardUserInfo
{
    public string Sub { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public string? TenantId { get; set; }

    public List<string> Roles { get; set; } = new();

    public List<string> Permissions { get; set; } = new();

    public List<string> Scope { get; set; } = new();

    public Dictionary<string, string> UserMetadatas { get; set; } = new();

    public Dictionary<string, string> TenantMetadata { get; set; } = new();

    protected AbsAsgardUserInfo() { }

    public virtual void InitFromClaims(IEnumerable<Claim> claims)
    {
        foreach (var claim in claims)
        {
            switch (claim.Type)
            {
                case "sub":
                    Sub = claim.Value;
                    break;
                case "user_id":
                    UserId = claim.Value;
                    break;
                case "tenant_id":
                    TenantId = claim.Value;
                    break;
                case "roles":
                    Roles = DeserializeList(claim.Value) ?? new();
                    break;
                case "permissions":
                    Permissions = DeserializeList(claim.Value) ?? new();
                    break;
                case "scope":
                    Scope = DeserializeList(claim.Value) ?? new();
                    break;
                case "userMetadatas":
                    UserMetadatas = DeserializeDictionary(claim.Value) ?? new();
                    break;
                case "tenantMetadata":
                    TenantMetadata = DeserializeDictionary(claim.Value) ?? new();
                    break;
            }
        }
    }

    public virtual IEnumerable<Claim> ToClaims()
    {
        var claims = new List<Claim>
        {
            new("sub", Sub)
        };

        if (!string.IsNullOrEmpty(UserId))
            claims.Add(new("user_id", UserId));
        if (!string.IsNullOrEmpty(TenantId))
            claims.Add(new("tenant_id", TenantId));
        if (Roles.Count > 0)
            claims.Add(new("roles", System.Text.Json.JsonSerializer.Serialize(Roles, JsonSerializerOptionsFactory.Default)));
        if (Permissions.Count > 0)
            claims.Add(new("permissions", System.Text.Json.JsonSerializer.Serialize(Permissions, JsonSerializerOptionsFactory.Default)));
        if (Scope.Count > 0)
            claims.Add(new("scope", System.Text.Json.JsonSerializer.Serialize(Scope, JsonSerializerOptionsFactory.Default)));
        if (UserMetadatas.Count > 0)
            claims.Add(new("userMetadatas", System.Text.Json.JsonSerializer.Serialize(UserMetadatas, JsonSerializerOptionsFactory.Default)));
        if (TenantMetadata.Count > 0)
            claims.Add(new("tenantMetadata", System.Text.Json.JsonSerializer.Serialize(TenantMetadata, JsonSerializerOptionsFactory.Default)));

        return claims;
    }

    private static List<string>? DeserializeList(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptionsFactory.Default);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string>? DeserializeDictionary(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonSerializerOptionsFactory.Default);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
