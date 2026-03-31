---
name: asgard-security
description: Asgard 安全能力 skill。Use when working with Asgard encryption, password hashing, key generation, sensitive data handling, Asgard.Encryption configuration, or deciding how to use Encryption, PasswordHasher, and KeyGenerator from AbsAsgardContext.
---

# Asgard Security

## 作用

定义 Asgard 提供的安全核心能力：加密、密码哈希、密钥生成。这些能力都通过 `AbsAsgardContext` 统一访问，支持模块级启用/禁用，未启用时优雅降级。

## 什么时候使用

- **需要加密敏感数据** - 使用 `IEncryptionService` 加密存储敏感配置
- **需要存储用户密码** - 使用 `IPasswordHasher` 进行 BCrypt 哈希
- **需要生成加密密钥** - 使用 `IKeyGenerator` 生成安全随机密钥
- **需要校验密码** - 使用 BCrypt 验证哈希并检测是否需要升级
- **需要通过 Context 获取安全能力** - 通过 `AbsAsgardContext` 属性获取

## 能力分类

| 能力 | 接口 | 作用 | 场景 |
|------|------|------|------|
| `Encryption` | `IEncryptionService` | AES 对称加密/解密、MD5 哈希计算 | 敏感配置加密存储、数据完整性校验 |
| `PasswordHasher` | `IPasswordHasher` | BCrypt 密码哈希、验证 | 用户密码存储、登录验证 |
| `KeyGenerator` | `IKeyGenerator` | AES、HMAC 密钥生成 | 生成新的加密密钥 |

## 获取方式

所有安全能力都通过 `AbsAsgardContext` 获取，属性可为 null（模块未启用时），必须做空检查后使用：

| 属性 | 说明 |
|------|------|
| `AbsAsgardContext.Encryption` | AES 加密/解密服务，模块未启用时为 null |
| `AbsAsgardContext.PasswordHasher` | BCrypt 密码哈希服务，模块未启用时为 null |
| `AbsAsgardContext.KeyGenerator` | 密钥生成服务，模块未启用时为 null |

## 推荐用法

| 操作 | 推荐做法 |
|------|----------|
| 加密敏感数据 | 通过 `Encryption.Encrypt()` 加密，不明文存储 |
| 用户注册密码 | 通过 `PasswordHasher.Hash()` 生成哈希存储，不存储明文 |
| 用户登录验证 | 通过 `PasswordHasher.Verify(password, hash, out needsRehash)` 验证，自动检测是否需要升级 |
| 生成新密钥 | 通过 `KeyGenerator` 生成，不要手写弱随机生成 |
| MD5 哈希 | 只有兼容性场景用，不用于密码安全 |

## 工作因子推荐（BCrypt）

| 工作因子 | 适用场景 | 性能 |
|----------|----------|------|
| 4-6 | 测试环境 | 快，但不安全 |
| 10-12 | 生产环境推荐 | 约 100-400ms，兼顾安全性能 |
| 13-15 | 高安全场景 | 较慢，但更安全 |
| 16+ | 极高安全要求 | 很慢，只有高安全场景使用 |

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

## 推荐做法

- 通过 `AbsAsgardContext` 获取安全能力，始终做空检查，支持模块动态启用/禁用
- 敏感数据加密存储，不要明文存储在数据库
- 密码永远不存储明文，始终 BCrypt 哈希
- 使用 BCrypt 内置随机盐，不需要自己加盐
- 验证密码时检查 `needsRehash`，自动升级旧哈希
- MD5 只用于兼容性场景，不用于密码安全

## 不要这样做

❌ 不要明文存储密码，始终哈希存储

❌ 不要把 MD5 当作密码哈希算法，MD5 不安全

❌ 不要自己实现随机密钥生成，始终使用 `KeyGenerator`

❌ 不要硬编码密钥在源码中，除非明确演示示例并接受风险

❌ 不要跳过 `needsRehash` 检查，它会自动帮你升级工作因子保持安全性

❌ 不要忽略 `null` 检查，安全模块可以被禁用，必须支持优雅降级

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `IEncryptionService.cs` - 加密服务接口
- `IPasswordHasher.cs` - 密码哈希接口
- `IKeyGenerator.cs` - 密钥生成接口

代码范本请参考 `templates/` 目录：
- `EncryptDecryptViaContext.cs.template` - 加密解密使用范本
- `PasswordHashVerify.cs.template` - 密码哈希验证范本
- `GenerateKeys.cs.template` - 密钥生成范本
