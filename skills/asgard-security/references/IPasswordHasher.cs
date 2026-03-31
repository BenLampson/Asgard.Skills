namespace Asgard.Abstractions.Security;

/// <summary>
/// 密码哈希器接口，提供基于 BCrypt 算法的密码安全存储和验证功能。
/// </summary>
/// <remarks>
/// <para>
/// 本接口定义了密码哈希服务的基础功能，包括：
/// <list type="bullet">
///   <item><description>密码哈希（可配置工作因子）</description></item>
///   <item><description>密码验证</description></item>
///   <item><description>哈希格式校验</description></item>
///   <item><description>重新哈希检测</description></item>
/// </list>
/// </para>
/// <para>
/// BCrypt 是一种专为密码存储设计的哈希算法，具有如下特点：
/// <list type="bullet">
///   <item><description>内置随机盐，每次哈希结果不同</description></item>
///   <item><description>计算成本可调，可抵抗暴力破解</description></item>
///   <item><description>输出长度固定为 60 个字符</description></item>
///   <item><description>抗 GPU/ASIC 攻击能力强</description></item>
/// </list>
/// </para>
/// <para>
/// <b>使用示例：</b>
/// <code><![CDATA[
/// // 通过依赖注入获取服务
/// var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
/// 
/// // 注册用户时哈希密码
/// string password = "用户密码";
/// string hash = passwordHasher.Hash(password);
/// // 将 hash 存储到数据库
/// 
/// // 登录时验证密码
/// bool isValid = passwordHasher.Verify(password, hash);
/// if (isValid)
/// {
///     // 登录成功
///     if (passwordHasher.Verify(password, hash, out bool needsRehash) && needsRehash)
///     {
///         // 工作因子需要提升，重新哈希密码
///         string newHash = passwordHasher.Hash(password);
///         // 更新数据库中的哈希值
///     }
/// }
/// ]]></code>
/// </para>
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// 对密码进行哈希处理，使用默认工作因子。
    /// </summary>
    /// <param name="password">要哈希的明文密码。</param>
    /// <returns>60 字符的 BCrypt 哈希字符串。</returns>
    /// <remarks>
    /// <para>
    /// 每次调用都会生成不同的哈希值（因为盐是随机的）。
    /// 但都可以通过 <see cref="Verify(string, string)"/> 方法验证。
    /// </para>
    /// <para>
    /// <b>哈希流程：</b>
    /// <list type="number">
    ///   <item><description>校验输入密码不为 null 或空字符串</description></item>
    ///   <item><description>使用 BCrypt 算法和默认工作因子进行哈希</description></item>
    ///   <item><description>返回 60 字符的哈希字符串</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>输出格式：</b>
    /// <c>$2a$11$randomsalt22charactersxxx/hashresult31charactersxxxxx</c>
    /// </para>
    /// <para>
    /// <b>默认工作因子：</b>
    /// 推荐为 10-12，响应时长约 100-400ms，兼顾安全性和性能。
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="password"/> 为 null 或空字符串时抛出。</exception>
    string Hash(string password);

    /// <summary>
    /// 对密码进行哈希处理，使用指定的工作因子。
    /// </summary>
    /// <param name="password">要哈希的明文密码。</param>
    /// <param name="workFactor">工作因子（计算成本），范围为 4-31。值越大越安全但越慢。</param>
    /// <returns>60 字符的 BCrypt 哈希字符串。</returns>
    /// <remarks>
    /// <para>
    /// 工作因子决定了迭代次数（2^workFactor 次）。
    /// </para>
    /// <para>
    /// <b>工作因子建议：</b>
    /// <list type="bullet">
    ///   <item><description>4-6：测试环境，快速但不安全</description></item>
    ///   <item><description>10-12：生产环境推荐</description></item>
    ///   <item><description>13-15：高安全场景</description></item>
    ///   <item><description>16+：极高安全要求，但性能影响显著</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>性能考量：</b>
    /// 工作因子每增加 1，计算时间大幅增加。请根据硬件性能和安全需求选择合适的值。
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="password"/> 为 null 或空字符串时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="workFactor"/> 不在有效范围（4-31）内时抛出。</exception>
    string Hash(string password, int workFactor);

    /// <summary>
    /// 验证密码是否与给定的哈希值匹配。
    /// </summary>
    /// <param name="password">要验证的明文密码。</param>
    /// <param name="hash">之前存储的 BCrypt 哈希值。</param>
    /// <returns>如果密码匹配则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    /// <remarks>
    /// <para>
    /// 此方法会自动从哈希值中提取盐和工作因子进行验证。
    /// 无需额外存储这些信息。
    /// </para>
    /// <para>
    /// <b>验证流程：</b>
    /// <list type="number">
    ///   <item><description>校验输入参数有效性</description></item>
    ///   <item><description>检查哈希格式是否正确</description></item>
    ///   <item><description>使用 BCrypt 算法验证密码</description></item>
    ///   <item><description>返回验证结果</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>错误处理：</b>
    /// 如果哈希格式无效或验证过程中发生错误，方法应返回 <c>false</c>。
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="password"/> 或 <paramref name="hash"/> 为 null 或空字符串时抛出。</exception>
    bool Verify(string password, string hash);

    /// <summary>
    /// 验证密码并检查是否需要重新哈希。
    /// </summary>
    /// <param name="password">要验证的明文密码。</param>
    /// <param name="hash">之前存储的 BCrypt 哈希值。</param>
    /// <param name="needsRehash">输出参数，指示是否需要使用新的工作因子重新哈希密码。</param>
    /// <returns>如果密码匹配则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    /// <remarks>
    /// <para>
    /// 当工作因子需要提升时（例如策略升级后），可使用此方法检测旧哈希值，
    /// 并在验证成功后重新哈希密码。
    /// </para>
    /// <para>
    /// <b>重新哈希场景：</b>
    /// <list type="bullet">
    ///   <item><description>哈希中的工作因子低于当前默认值</description></item>
    ///   <item><description>算法版本需要升级</description></item>
    ///   <item><description>安全策略变更</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>使用模式：</b>
    /// <code>
    /// if (passwordHasher.Verify(password, storedHash, out bool needsRehash))
    /// {
    ///     if (needsRehash)
    ///     {
    ///         // 使用当前默认工作因子重新哈希
    ///         string newHash = passwordHasher.Hash(password);
    ///         // 更新数据库中的哈希值
    ///     }
    ///     // 登录成功
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="password"/> 或 <paramref name="hash"/> 为 null 或空字符串时抛出。</exception>
    bool Verify(string password, string hash, out bool needsRehash);

    /// <summary>
    /// 校验字符串是否为有效的 BCrypt 哈希格式。
    /// </summary>
    /// <param name="hash">要验证的字符串。</param>
    /// <returns>如果是有效的 BCrypt 哈希格式则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    /// <remarks>
    /// <para>
    /// 此方法仅检查格式，不验证哈希是否对应某个密码。
    /// </para>
    /// <para>
    /// <b>校验规则：</b>
    /// <list type="bullet">
    ///   <item><description>字符串不能为空或 null</description></item>
    ///   <item><description>长度必须为 60 个字符</description></item>
    ///   <item><description>必须以有效的 BCrypt 前缀开头（$2a$, $2b$, $2y$）</description></item>
    ///   <item><description>格式必须符合 BCrypt 规范</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>BCrypt 格式：</b>
    /// <list type="bullet">
    ///   <item><description>$2a$ - 原始 BCrypt 算法</description></item>
    ///   <item><description>$2b$ - 修正了 2011 年发现的漏洞</description></item>
    ///   <item><description>$2y$ - 兼容某些老系统</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    bool IsValidHashFormat(string? hash);
}
