## 代码示例

### 加密解密（通过 Context）

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <returns>操作结果</returns>
public async Task<{ResultType}?> {MethodName}({ParameterType} {ParameterName})
{
    if (AsgardContext.Encryption == null)
    {
        // 加密模块未启用，降级处理
        return null;
    }

    var encrypted = AsgardContext.Encryption.Encrypt({plainText});
    // 保存 encrypted 到数据库...
    return encrypted;
}

/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="encrypted">{encryptedSummary}</param>
/// <returns>解密后的数据</returns>
public async Task<string?> {DecryptMethodName}(string encrypted)
{
    if (AsgardContext.Encryption == null)
    {
        // 加密模块未启用，降级处理
        return null;
    }

    var decrypted = AsgardContext.Encryption.Decrypt(encrypted);
    return decrypted;
}
```

### 用户注册与登录（密码哈希）

```csharp
/// <summary>
/// 用户注册，密码哈希
/// </summary>
/// <param name="email">邮箱</param>
/// <param name="password">明文密码</param>
/// <returns>注册结果</returns>
public async Task<Result> RegisterAsync(string email, string password)
{
    if (AsgardContext.PasswordHasher == null)
    {
        // 密码哈希模块未启用，降级处理（直接存储明文是不安全的）
        return Result.Fail("密码哈希服务未启用");
    }

    var passwordHash = AsgardContext.PasswordHasher.Hash(password);
    var user = new User { Email = email, PasswordHash = passwordHash };
    await _userRepository.AddAsync(user);
    return Result.Ok();
}

/// <summary>
/// 用户登录，密码验证
/// </summary>
/// <param name="email">邮箱</param>
/// <param name="password">明文密码</param>
/// <returns>登录结果</returns>
public async Task<Result<LoginResponse>> LoginAsync(string email, string password)
{
    if (AsgardContext.PasswordHasher == null)
    {
        // 密码哈希模块未启用
        return Result.Fail<LoginResponse>("密码验证服务未启用");
    }

    var user = await _userRepository.FindByEmailAsync(email);
    if (user == null)
    {
        return Result.Fail<LoginResponse>("用户不存在");
    }

    if (!AsgardContext.PasswordHasher.Verify(password, user.PasswordHash, out var needsRehash))
    {
        return Result.Fail<LoginResponse>("密码不正确");
    }

    // 如果密码哈希需要升级工作因子，自动重新哈希
    if (needsRehash)
    {
        var newHash = AsgardContext.PasswordHasher.Hash(password);
        await _userRepository.UpdatePasswordHashAsync(user.Id, newHash);
    }

    // 生成登录凭证...
    return Result.Ok(new LoginResponse(token));
}
```

### 密钥生成

```csharp
/// <summary>
/// 生成新的 AES 密钥和 IV
/// </summary>
/// <returns>密钥和 IV（Base64 编码）</returns>
public (string Key, string Iv) GenerateAesKey()
{
    if (AsgardContext.KeyGenerator == null)
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
    if (AsgardContext.KeyGenerator == null)
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
    if (AsgardContext.KeyGenerator == null)
    {
        throw new InvalidOperationException("密钥生成服务未启用");
    }

    return AsgardContext.KeyGenerator.CreateRandomKey(keySizeInBytes);
}
```
