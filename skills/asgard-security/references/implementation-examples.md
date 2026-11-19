# 通过 Context 使用安全能力

## 加解密

```csharp
/// <summary>加密字段，缺少配置时由框架报告模块关闭。</summary>
public string? EncryptField(string? plainText) => AsgardContext.Encryption.Encrypt(plainText);

/// <summary>解密字段。</summary>
public string? DecryptField(string? encrypted) => AsgardContext.Encryption.Decrypt(encrypted);
```

## 密码哈希与验证

```csharp
/// <summary>生成用户密码的哈希。</summary>
public string HashPassword(string password) => AsgardContext.PasswordHasher.Hash(password);

/// <summary>验证密码，并指示调用方是否应在成功后更新旧哈希。</summary>
public bool VerifyPassword(string password, string storedHash, out bool needsRehash) =>
    AsgardContext.PasswordHasher.Verify(password, storedHash, out needsRehash);
```

## 生成密钥

```csharp
/// <summary>
/// 生成新的 AES 密钥和 IV
/// </summary>
/// <returns>密钥和 IV（Base64 编码）</returns>
public (string Key, string Iv) GenerateAesKey()
{
    if (AsgardContext.KeyGenerator.Status == AsgardFeatureStatus.Disabled)
    {
        throw new InvalidOperationException("密钥生成服务未启用");
    }

    return AsgardContext.KeyGenerator.CreateAesKeyAndIv();
}

/// <summary>
/// 生成新的 HMACSHA256 密钥
/// </summary>
/// <returns>Base64 编码的密钥</returns>
public string GenerateHmacKey()
{
    if (AsgardContext.KeyGenerator.Status == AsgardFeatureStatus.Disabled)
    {
        throw new InvalidOperationException("密钥生成服务未启用");
    }

    return AsgardContext.KeyGenerator.CreateHmacSha256Key();
}

/// <summary>
/// 生成指定长度的随机密钥
/// </summary>
/// <param name="keySizeInBytes">密钥长度（字节数）</param>
/// <returns>Base64 编码的密钥</returns>
public string GenerateRandomKey(int keySizeInBytes)
{
    if (AsgardContext.KeyGenerator.Status == AsgardFeatureStatus.Disabled)
    {
        throw new InvalidOperationException("密钥生成服务未启用");
    }

    return AsgardContext.KeyGenerator.CreateRandomKey(keySizeInBytes);
}
```
