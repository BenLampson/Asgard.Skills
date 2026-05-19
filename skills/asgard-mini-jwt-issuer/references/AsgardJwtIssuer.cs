namespace Asgard.Heimdall.JwtSigning;

/// <summary>
/// 表示默认 Asgard JWT 签发器。
/// </summary>
public sealed class AsgardJwtIssuer : IAsgardJwtIssuer, IDisposable
{
    private static readonly JwtSecurityTokenHandler _tokenHandler = new()
    {
        MapInboundClaims = false
    };

    private readonly AsgardJwtSigningOptions _options;
    private readonly SecurityKey _signingKey;
    private readonly SecurityKey _publicKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly RSA? _ownedSigningRsa;
    private readonly RSA? _ownedPublicRsa;

    /// <summary>
    /// 初始化 AsgardJwtIssuer 实例。
    /// </summary>
    public AsgardJwtIssuer(AsgardJwtSigningOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        (_signingKey, _publicKey, _ownedSigningRsa, _ownedPublicRsa) = CreateKeys(options);
        _signingCredentials = new SigningCredentials(_signingKey, options.Algorithm);
    }

    /// <inheritdoc />
    public AsgardJwtIssueResult Issue(AbsAsgardUserInfo userInfo, AsgardJwtIssueOptions? issueOptions = null)
    {
        ArgumentNullException.ThrowIfNull(userInfo);

        var subject = new AsgardJwtSubject
        {
            Subject = userInfo.Sub,
            UserId = userInfo.UserId,
            TenantId = userInfo.TenantId?.ToString(),
            ClientId = userInfo.ClientId,
            Roles = userInfo.Roles,
            Permissions = userInfo.Permissions,
            Scope = userInfo.Scope,
            UserMetadatas = userInfo.UserMetadatas,
            TenantMetadata = userInfo.TenantMetadata
        };

        return Issue(subject, issueOptions);
    }

    /// <inheritdoc />
    public AsgardJwtIssueResult Issue(AsgardJwtSubject subject, AsgardJwtIssueOptions? issueOptions = null)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ValidateOptions();

        var tokenType = string.IsNullOrWhiteSpace(subject.TokenType)
            ? _options.DefaultTokenType
            : subject.TokenType.Trim();
        ValidateSubject(subject, tokenType);

        var issuedAt = issueOptions?.IssuedAt ?? DateTimeOffset.UtcNow;
        var lifetime = issueOptions?.Lifetime ?? _options.AccessTokenLifetime;
        if (lifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("访问令牌生命周期必须大于 0。");
        }

        var expiresAt = issuedAt.Add(lifetime);
        var audience = issueOptions?.Audience ?? _options.Audience;
        var jti = string.IsNullOrWhiteSpace(issueOptions?.Jti)
            ? Guid.NewGuid().ToString("N")
            : issueOptions.Jti.Trim();

        var identity = BuildClaimsIdentity(subject, tokenType, jti);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = audience,
            Subject = identity,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentials
        };

        var accessToken = _tokenHandler.CreateEncodedJwt(descriptor);
        return new AsgardJwtIssueResult(accessToken, "Bearer", (int)lifetime.TotalSeconds, issuedAt, expiresAt, jti);
    }

    /// <inheritdoc />
    public AsgardJwksDocument CreateJwksDocument()
    {
        var key = JsonWebKeyConverter.ConvertFromSecurityKey(_publicKey);
        key.Kid = _options.KeyId;
        key.Alg = _options.Algorithm;
        key.Use = "sig";
        return new AsgardJwksDocument([key]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _ownedSigningRsa?.Dispose();
        _ownedPublicRsa?.Dispose();
    }

    private static ClaimsIdentity BuildClaimsIdentity(AsgardJwtSubject subject, string tokenType, string jti)
    {
        var identity = new ClaimsIdentity("asgard_jwt", "name", ClaimTypes.Role);
        SetRequiredClaim(identity, AsgardClaimTypes.Sub, subject.Subject!);
        SetOptionalClaim(identity, AsgardClaimTypes.UserId, subject.UserId);
        SetOptionalClaim(identity, AsgardClaimTypes.TenantId, subject.TenantId);
        SetOptionalClaim(identity, AsgardClaimTypes.ClientId, subject.ClientId);
        SetRequiredClaim(identity, AsgardClaimTypes.TokenType, tokenType);
        SetRequiredClaim(identity, AsgardClaimTypes.Roles, SerializeArray(subject.Roles));
        SetRequiredClaim(identity, AsgardClaimTypes.Permissions, SerializeArray(subject.Permissions));
        SetRequiredClaim(identity, AsgardClaimTypes.Scope, SerializeArray(subject.Scope));
        SetRequiredClaim(identity, AsgardClaimTypes.UserMetadatas, SerializeObject(subject.UserMetadatas));
        SetRequiredClaim(identity, AsgardClaimTypes.TenantMetadata, SerializeObject(subject.TenantMetadata));
        SetRequiredClaim(identity, JwtRegisteredClaimNames.Jti, jti);

        SetOptionalClaim(identity, JwtRegisteredClaimNames.Name, subject.Name);
        SetOptionalClaim(identity, JwtRegisteredClaimNames.Email, subject.Email);
        SetOptionalClaim(identity, "phone_number", subject.PhoneNumber);
        SetOptionalClaim(identity, "sid", subject.SessionId);
        if (subject.AuthenticationTime is not null)
        {
            SetRequiredClaim(identity, "auth_time", subject.AuthenticationTime.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64);
        }

        return identity;
    }

    private static void ValidateSubject(AsgardJwtSubject subject, string tokenType)
    {
        if (string.IsNullOrWhiteSpace(subject.Subject))
        {
            throw new InvalidOperationException("JWT 主体必须提供 sub。");
        }

        if (string.Equals(tokenType, AsgardJwtConstants.BackendServiceTokenType, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(subject.ClientId))
            {
                throw new InvalidOperationException("后端服务令牌必须提供 client_id。");
            }

            if (!string.IsNullOrWhiteSpace(subject.UserId))
            {
                throw new InvalidOperationException("后端服务令牌不能包含 user_id。");
            }
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Issuer))
        {
            throw new InvalidOperationException("JWT 签发配置必须提供 Issuer。");
        }

        if (string.IsNullOrWhiteSpace(_options.Audience))
        {
            throw new InvalidOperationException("JWT 签发配置必须提供 Audience。");
        }

        if (string.IsNullOrWhiteSpace(_options.KeyId))
        {
            throw new InvalidOperationException("JWT 签发配置必须提供 KeyId。");
        }
    }

    private static (SecurityKey SigningKey, SecurityKey PublicKey, RSA? SigningRsa, RSA? PublicRsa) CreateKeys(AsgardJwtSigningOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SymmetricSecurityKey))
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SymmetricSecurityKey))
            {
                KeyId = options.KeyId
            };
            return (key, key, null, null);
        }

        if (string.IsNullOrWhiteSpace(options.RsaPrivateKeyPem))
        {
            throw new InvalidOperationException("JWT 签发配置必须提供 RsaPrivateKeyPem 或 SymmetricSecurityKey。");
        }

        var signingRsa = RSA.Create();
        signingRsa.ImportFromPem(options.RsaPrivateKeyPem);
        var signingKey = new RsaSecurityKey(signingRsa)
        {
            KeyId = options.KeyId
        };

        RSA publicRsa;
        if (string.IsNullOrWhiteSpace(options.RsaPublicKeyPem))
        {
            publicRsa = RSA.Create();
            publicRsa.ImportParameters(signingRsa.ExportParameters(false));
        }
        else
        {
            publicRsa = RSA.Create();
            publicRsa.ImportFromPem(options.RsaPublicKeyPem);
        }

        var publicKey = new RsaSecurityKey(publicRsa)
        {
            KeyId = options.KeyId
        };
        return (signingKey, publicKey, signingRsa, publicRsa);
    }

    private static void SetRequiredClaim(ClaimsIdentity identity, string type, string value, string valueType = ClaimValueTypes.String)
        => identity.AddClaim(new Claim(type, value, valueType));

    private static void SetOptionalClaim(ClaimsIdentity identity, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            identity.AddClaim(new Claim(type, value.Trim()));
        }
    }

    private static string SerializeArray(IEnumerable<string>? values)
        => JsonSerializer.Serialize(Normalize(values));

    private static string SerializeObject(IReadOnlyDictionary<string, string>? values)
        => JsonSerializer.Serialize(Normalize(values));

    private static IReadOnlyCollection<string> Normalize(IEnumerable<string>? values)
        => values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray() ?? [];

    private static IReadOnlyDictionary<string, string> Normalize(IReadOnlyDictionary<string, string>? values)
    {
        var results = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in values ?? new Dictionary<string, string>(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
            {
                continue;
            }

            results[entry.Key.Trim()] = entry.Value.Trim();
        }

        return results;
    }
}
