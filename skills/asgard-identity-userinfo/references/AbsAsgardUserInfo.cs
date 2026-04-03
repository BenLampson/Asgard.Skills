namespace Asgard.Abstractions.Identity;

/// <summary>
/// 框架级别的抽象用户信息基类，支持从 Claim[] 初始化和转换为 Claim[]。
/// </summary>
public abstract class AbsAsgardUserInfo
{
    public string Sub { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public string? TenantId { get; set; }

    public string? ClientId { get; set; }

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
                case AsgardClaimTypes.Sub:
                    Sub = claim.Value;
                    break;
                case AsgardClaimTypes.UserId:
                    UserId = claim.Value;
                    break;
                case AsgardClaimTypes.TenantId:
                    TenantId = claim.Value;
                    break;
                case AsgardClaimTypes.ClientId:
                    ClientId = claim.Value;
                    break;
                case AsgardClaimTypes.Roles:
                    Roles = DeserializeList(claim.Value) ?? new();
                    break;
                case AsgardClaimTypes.Permissions:
                    Permissions = DeserializeList(claim.Value) ?? new();
                    break;
                case AsgardClaimTypes.Scope:
                    Scope = DeserializeList(claim.Value) ?? new();
                    break;
                case AsgardClaimTypes.UserMetadatas:
                    UserMetadatas = DeserializeDictionary(claim.Value) ?? new();
                    break;
                case AsgardClaimTypes.TenantMetadata:
                    TenantMetadata = DeserializeDictionary(claim.Value) ?? new();
                    break;
            }
        }
    }

    public virtual IEnumerable<Claim> ToClaims()
    {
        var claims = new List<Claim>
        {
            new(AsgardClaimTypes.Sub, Sub)
        };

        if (!string.IsNullOrEmpty(UserId))
            claims.Add(new(AsgardClaimTypes.UserId, UserId));
        if (!string.IsNullOrEmpty(TenantId))
            claims.Add(new(AsgardClaimTypes.TenantId, TenantId));
        if (!string.IsNullOrEmpty(ClientId))
            claims.Add(new(AsgardClaimTypes.ClientId, ClientId));
        if (Roles.Count > 0)
            claims.Add(new(AsgardClaimTypes.Roles, System.Text.Json.JsonSerializer.Serialize(Roles, JsonSerializerOptionsFactory.Default)));
        if (Permissions.Count > 0)
            claims.Add(new(AsgardClaimTypes.Permissions, System.Text.Json.JsonSerializer.Serialize(Permissions, JsonSerializerOptionsFactory.Default)));
        if (Scope.Count > 0)
            claims.Add(new(AsgardClaimTypes.Scope, System.Text.Json.JsonSerializer.Serialize(Scope, JsonSerializerOptionsFactory.Default)));
        if (UserMetadatas.Count > 0)
            claims.Add(new(AsgardClaimTypes.UserMetadatas, System.Text.Json.JsonSerializer.Serialize(UserMetadatas, JsonSerializerOptionsFactory.Default)));
        if (TenantMetadata.Count > 0)
            claims.Add(new(AsgardClaimTypes.TenantMetadata, System.Text.Json.JsonSerializer.Serialize(TenantMetadata, JsonSerializerOptionsFactory.Default)));

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
