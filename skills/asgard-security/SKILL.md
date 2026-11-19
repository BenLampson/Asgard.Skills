---
name: asgard-security
description: "在 Asgard 中使用加密、密码哈希和密钥生成，处理 Encryption、PasswordHasher、KeyGenerator 与敏感数据。登录和授权分别用身份、授权 skill。"
---

# Asgard Security

## 作用

定义 Asgard 提供的安全核心能力：加密、密码哈希、密钥生成。这些能力都通过 `AbsAsgardContext` 统一访问，可通过各自的 `Status` 了解装配状态。

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

所有安全能力都通过 `AbsAsgardContext` 获取，模块关闭时 `Status` 为 `Disabled`。需要该能力的密码或加密操作会抛 `AsgardFeatureDisabledException`。

最小 Yggdrasil 自动装配 `PasswordHasher` 和 `KeyGenerator`。省略 `Asgard.Encryption.Key` 与 `Iv` 会关闭 AES。只提供其中一项、Base64 错误或字节长度不合要求时，宿主在构建时报告配置错误。仅调用 `AddAsgardContext()` 的空容器中三者都是 Disabled。[各能力默认状态](../asgard-context-usage/references/context-feature-matrix.md)

| 属性 | 说明 |
|------|------|
| `AbsAsgardContext.Encryption` | AES 加密/解密服务，关闭时可检查状态和配置 |
| `AbsAsgardContext.PasswordHasher` | BCrypt 密码哈希服务，关闭时可检查状态和配置 |
| `AbsAsgardContext.KeyGenerator` | 密钥生成服务，关闭时可检查状态和配置 |

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

需要编写该模块代码时，按场景读取 [实现示例](references/implementation-examples.md)，只采用与当前任务和目标版本匹配的示例。

## 推荐做法

- 通过 `AbsAsgardContext` 获取安全能力，按 `Status` 判断是否提供相应操作
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

密码校验、加密等操作是业务前提时，让模块关闭或运行失败的异常传播。

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `IEncryptionService.cs` - 加密服务接口
- `IPasswordHasher.cs` - 密码哈希接口
- `IKeyGenerator.cs` - 密钥生成接口

代码范本请参考 `templates/` 目录：
- `EncryptDecryptViaContext.cs.template` - 加密解密使用范本
- `PasswordHashVerify.cs.template` - 密码哈希验证范本
- `GenerateKeys.cs.template` - 密钥生成范本
