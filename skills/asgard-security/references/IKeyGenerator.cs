namespace Asgard.Abstractions.Security;

/// <summary>
/// 密钥生成器接口，提供多种加密算法所需的密钥生成能力。
/// </summary>
/// <remarks>
/// <para>
/// 本接口定义了密钥生成服务的基础功能，包括：
/// <list type="bullet">
///   <item><description>AES 密钥和初始化向量生成</description></item>
///   <item><description>HMACSHA256 密钥生成</description></item>
///   <item><description>通用随机密钥生成</description></item>
/// </list>
/// </para>
/// <para>
/// 所有生成的密钥均使用 Base64 编码，便于存储和传输。
/// 实现应使用加密安全的随机数生成器，确保密钥的随机性和安全性。
/// </para>
/// <para>
/// <b>使用示例：</b>
/// <code>
/// // 通过依赖注入获取服务
/// var keyGenerator = serviceProvider.GetRequiredService&lt;IKeyGenerator&gt;();
/// 
/// // 生成 AES 密钥和初始化向量
/// var (aesKey, aesIv) = keyGenerator.CreateAesKeyAndIv();
/// 
/// // 生成 HMACSHA256 密钥
/// string hmacKey = keyGenerator.CreateHmacSha256Key();
/// 
/// // 生成指定长度的随机密钥
/// string randomKey = keyGenerator.CreateRandomKey(32); // 256 位密钥
/// </code>
/// </para>
/// </remarks>
public interface IKeyGenerator
{
    /// <summary>
    /// 创建新的 AES 加密密钥和初始化向量。
    /// </summary>
    /// <returns>
    /// 包含 Base64 编码密钥和初始化向量的元组：
    /// <list type="bullet">
    ///   <item><description>Key: 256 位（32 字节）密钥的 Base64 编码字符串</description></item>
    ///   <item><description>Iv: 128 位（16 字节）初始化向量的 Base64 编码字符串</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// 每次调用都会生成新的随机密钥和初始化向量。
    /// 生成的密钥可用于 <see cref="IEncryptionService"/> 实现。
    /// </para>
    /// <para>
    /// <b>生成流程：</b>
    /// <list type="number">
    ///   <item><description>创建 AES 算法实例</description></item>
    ///   <item><description>获取自动生成的密钥和初始化向量</description></item>
    ///   <item><description>将字节数组转换为 Base64 编码字符串</description></item>
    ///   <item><description>返回密钥和初始化向量</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>密钥格式：</b>
    /// <list type="bullet">
    ///   <item><description>密钥长度：256 位（32 字节）</description></item>
    ///   <item><description>初始化向量长度：128 位（16 字节）</description></item>
    ///   <item><description>算法：AES（Advanced Encryption Standard）</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    (string Key, string Iv) CreateAesKeyAndIv();

    /// <summary>
    /// 创建新的 HMACSHA256 密钥。
    /// </summary>
    /// <returns>Base64 编码的 HMACSHA256 密钥字符串。</returns>
    /// <remarks>
    /// <para>
    /// HMACSHA256 使用 256 位密钥，适用于消息认证和完整性校验。
    /// </para>
    /// <para>
    /// <b>生成流程：</b>
    /// <list type="number">
    ///   <item><description>创建 HMACSHA256 算法实例</description></item>
    ///   <item><description>获取自动生成的密钥</description></item>
    ///   <item><description>将字节数组转换为 Base64 编码字符串</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>应用场景：</b>
    /// <list type="bullet">
    ///   <item><description>API 请求签名验证</description></item>
    ///   <item><description>消息完整性校验</description></item>
    ///   <item><description>JWT 令牌签名</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    string CreateHmacSha256Key();

    /// <summary>
    /// 创建指定长度的随机密钥。
    /// </summary>
    /// <param name="keySizeInBytes">密钥长度（字节数）。</param>
    /// <returns>Base64 编码的随机密钥字符串。</returns>
    /// <remarks>
    /// <para>
    /// 此方法使用加密安全的随机数生成器，适用于生成各种长度的密钥。
    /// </para>
    /// <para>
    /// <b>常用密钥长度：</b>
    /// <list type="bullet">
    ///   <item><description>16 字节（128 位）- AES-128</description></item>
    ///   <item><description>24 字节（192 位）- AES-192</description></item>
    ///   <item><description>32 字节（256 位）- AES-256（推荐）</description></item>
    ///   <item><description>64 字节（512 位）- 高安全场景</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>生成流程：</b>
    /// <list type="number">
    ///   <item><description>校验密钥长度参数</description></item>
    ///   <item><description>使用加密安全的随机数生成器生成指定长度的字节数组</description></item>
    ///   <item><description>将字节数组转换为 Base64 编码字符串</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>参数校验：</b>
    /// <list type="bullet">
    ///   <item><description>密钥长度必须大于 0</description></item>
    ///   <item><description>密钥长度不应超过 1024 字节（安全限制）</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="keySizeInBytes"/> 小于等于 0 或超过安全限制时抛出。</exception>
    string CreateRandomKey(int keySizeInBytes);
}
