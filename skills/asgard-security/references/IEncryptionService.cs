namespace Asgard.Abstractions.Security;


/// <summary>
/// 加密服务接口，提供对称加密、哈希计算等安全功能。
/// </summary>
/// <remarks>
/// <para>
/// 本接口定义了加密服务的基础功能，包括：
/// <list type="bullet">
///   <item><description>AES 对称加密与解密</description></item>
///   <item><description>MD5 哈希计算</description></item>
///   <item><description>基于启动期配置的密钥和初始化向量执行加解密</description></item>
/// </list>
/// </para>
/// <para>
/// 实现本接口的服务应保证线程安全，因为加密操作可能被多个线程同时调用。
/// </para>
/// <para>
/// <b>使用示例：</b>
/// <code>
/// // 通过依赖注入获取服务
/// var encryptionService = serviceProvider.GetRequiredService&lt;IEncryptionService&gt;();
/// 
/// // 加密数据
/// string encrypted = encryptionService.Encrypt("敏感数据");
/// 
/// // 解密数据
/// string decrypted = encryptionService.Decrypt(encrypted);
/// 
/// // 计算 MD5 哈希
/// string hash = encryptionService.ComputeMd5Hash("要哈希的文本");
/// </code>
/// </para>
/// </remarks>
public interface IEncryptionService
{
    /// <summary>
    /// 使用 AES 算法加密指定的明文字符串。
    /// </summary>
    /// <param name="plainText">要加密的明文。</param>
    /// <returns>
    /// 加密后的十六进制格式字符串；如果输入为空则返回 <c>null</c>。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 返回的字符串为十六进制格式，每个字节用两个字符表示。
    /// 例如：<c>"Hello"</c> 可能返回 <c>"A1B2C3D4E5..."</c>
    /// </para>
    /// <para>
    /// <b>加密流程：</b>
    /// <list type="number">
    ///   <item><description>校验输入参数</description></item>
    ///   <item><description>创建 AES 加密器实例</description></item>
    ///   <item><description>使用配置的密钥和初始化向量</description></item>
    ///   <item><description>执行加密操作</description></item>
    ///   <item><description>将结果转换为十六进制字符串</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>异常处理：</b>
    /// 如果加密过程中发生异常，方法应返回 <c>null</c> 而不是抛出异常，
    /// 以确保调用方可以处理加密失败的情况。
    /// </para>
    /// </remarks>
    string? Encrypt(string? plainText);

    /// <summary>
    /// 使用 AES 算法解密指定的密文字符串。
    /// </summary>
    /// <param name="cipherText">要解密的十六进制格式密文。</param>
    /// <returns>
    /// 解密后的明文字符串；如果输入为空或解密失败则返回 <c>null</c>。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 输入应为 <see cref="Encrypt"/> 方法返回的十六进制格式字符串。
    /// </para>
    /// <para>
    /// <b>解密流程：</b>
    /// <list type="number">
    ///   <item><description>校验输入参数</description></item>
    ///   <item><description>将十六进制字符串转换为字节数组</description></item>
    ///   <item><description>创建 AES 解密器实例</description></item>
    ///   <item><description>使用配置的密钥和初始化向量</description></item>
    ///   <item><description>执行解密操作</description></item>
    ///   <item><description>将结果转换为字符串</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>异常处理：</b>
    /// 如果解密过程中发生异常（如无效的密文格式），方法应返回 <c>null</c>。
    /// </para>
    /// </remarks>
    string? Decrypt(string? cipherText);

    /// <summary>
    /// 计算指定字符串的 MD5 哈希值。
    /// </summary>
    /// <param name="text">要计算哈希的文本。</param>
    /// <returns>32 位小写十六进制格式的 MD5 哈希值。</returns>
    /// <remarks>
    /// <para>
    /// MD5 是一种广泛使用的密码散列函数，可产生 128 位的哈希值。
    /// </para>
    /// <para>
    /// <b>警告：</b>MD5 不适用于安全敏感场景，建议使用 SHA-256 或更强的算法。
    /// 本方法主要用于非安全场景，如生成缓存键或校验数据完整性等。
    /// </para>
    /// <para>
    /// <b>计算流程：</b>
    /// <list type="number">
    ///   <item><description>将字符串编码为 UTF-8 字节序列</description></item>
    ///   <item><description>使用 MD5 算法计算哈希值</description></item>
    ///   <item><description>将哈希值转换为十六进制字符串</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    string ComputeMd5Hash(string text);
}
