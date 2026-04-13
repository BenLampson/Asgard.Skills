namespace Asgard.Abstractions;

/// <summary>
/// Asgard 框架的统一上下文抽象类，提供所有模块的统一访问入口。
/// </summary>
/// <remarks>
/// <para>
/// 此类作为 Asgard 框架的统一访问点，解决了以下问题：
/// <list type="bullet">
///   <item><description>简化模块访问：用户无需记住多个接口，通过 Context 即可访问所有功能</description></item>
///   <item><description>避免循环依赖：各模块通过 Context 获取依赖，避免模块间直接引用</description></item>
///   <item><description>可选功能支持：未注册的服务返回 null，用户可优雅降级处理</description></item>
/// </list>
/// </para>
/// <para>
/// <b>生命周期：</b>
/// Context 注册为 Scoped 生命周期，每次请求创建新实例。
/// 这确保了在租户隔离等场景下的正确行为。
/// </para>
/// <para>
/// <b>使用示例：</b>
/// <code>
/// public class OrderService
/// {
///     private readonly AbsAsgardContext _context;
///     
///     public MyService(AbsAsgardContext context)
///     {
///         _context = context;
///     }
///     
///     public async Task&lt;Order&gt; GetOrderAsync(int id)
///     {
///         // 先查缓存
///         var cached = await _context.Cache?.GetAsync&lt;Order&gt;($"order:{id}")!;
///         if (cached != null) return cached;
///         
///         // 查数据库...
///         
///         // 写入缓存
///         await _context.Cache?.SetAsync($"order:{id}", order)!;
///         
///         // 发送消息
///         await _context.MessageQueue?.PublishAsync("orders", order)!;
///         
///         return order;
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class AbsAsgardContext
{
    /// <summary>
    /// 获取多级缓存服务。
    /// </summary>
    /// <value>
    /// 多级缓存服务实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 多级缓存结合本地内存缓存（一级）和分布式缓存（二级），平衡性能与一致性。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>热点数据缓存</description></item>
    ///   <item><description>配置信息缓存</description></item>
    ///   <item><description>用户会话缓存</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IMultiLevelCache? Cache { get; }

    /// <summary>
    /// 获取压缩服务。
    /// </summary>
    /// <value>
    /// 压缩服务实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 压缩服务提供基于 Brotli 算法的数据压缩和解压缩功能。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>大文件数据压缩存储</description></item>
    ///   <item><description>网络传输数据压缩</description></item>
    ///   <item><description>日志数据压缩归档</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract ICompressionService? Compression { get; }

    /// <summary>
    /// 获取租户作用域工厂。
    /// </summary>
    /// <value>
    /// 租户作用域工厂实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 租户作用域工厂用于在后台任务、消息处理等非 HTTP 场景中创建租户隔离作用域。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>后台任务中的租户隔离</description></item>
    ///   <item><description>消息处理中的租户隔离</description></item>
    ///   <item><description>定时任务中的租户隔离</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract ITenantScopeFactory? TenantScopeFactory { get; }

    /// <summary>
    /// 获取身份上下文。
    /// </summary>
    /// <value>
    /// 身份上下文实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 身份上下文提供当前请求的身份信息，包括租户标识、用户信息、用户类型等。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>获取当前登录用户信息</description></item>
    ///   <item><description>获取当前租户标识</description></item>
    ///   <item><description>判断用户类型和权限</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IAsgardIdentityContext? IdentityContext { get; }

    /// <summary>
    /// 获取任务调度器。
    /// </summary>
    /// <value>
    /// 任务调度器实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 任务调度器提供作业的创建、删除、暂停、恢复和触发等管理功能。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>定时任务调度</description></item>
    ///   <item><description>周期性数据处理</description></item>
    ///   <item><description>延迟任务执行</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IJobScheduler? JobScheduler { get; }

    /// <summary>
    /// 获取消息队列。
    /// </summary>
    /// <value>
    /// 消息队列实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
/// 消息队列提供消息的发布和订阅功能，当前默认基于 RabbitMQ 实现。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>异步消息处理</description></item>
    ///   <item><description>事件驱动架构</description></item>
    ///   <item><description>服务间通信</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IMessageQueue? MessageQueue { get; }

    /// <summary>
    /// 获取加密服务。
    /// </summary>
    /// <value>
    /// 加密服务实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 加密服务提供 AES 对称加密、MD5 哈希计算等安全功能。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>敏感数据加密存储</description></item>
    ///   <item><description>配置信息加密</description></item>
    ///   <item><description>数据完整性校验</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IEncryptionService? Encryption { get; }

    /// <summary>
    /// 获取密码哈希器。
    /// </summary>
    /// <value>
    /// 密码哈希器实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 密码哈希器提供基于 BCrypt 算法的密码安全存储和验证功能。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>用户密码哈希存储</description></item>
    ///   <item><description>密码验证</description></item>
    ///   <item><description>密码强度提升</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IPasswordHasher? PasswordHasher { get; }

    /// <summary>
    /// 获取密钥生成器。
    /// </summary>
    /// <value>
    /// 密钥生成器实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 密钥生成器提供各种加密算法所需的密钥生成功能。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>生成 AES 加密密钥</description></item>
    ///   <item><description>生成 HMAC 签名密钥</description></item>
    ///   <item><description>生成随机盐值</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IKeyGenerator? KeyGenerator { get; }

    /// <summary>
    /// 获取系统配置。
    /// </summary>
    /// <value>
    /// 系统配置实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 系统配置提供配置验证功能，所有配置类都应实现此接口。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>配置验证</description></item>
    ///   <item><description>配置加载后处理</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract ISystemConfig? SystemConfig { get; }

    /// <summary>
    /// 获取通配符匹配器。
    /// </summary>
    /// <value>
    /// 通配符匹配器实例，如果未注册则返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 通配符匹配器提供高性能的通配符模式匹配功能，支持 * 通配符。
    /// </para>
    /// <para>
    /// <b>使用场景：</b>
    /// <list type="bullet">
    ///   <item><description>文件路径过滤</description></item>
    ///   <item><description>URL 路由匹配</description></item>
    ///   <item><description>字符串模式搜索</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract IWildcardMatcher? WildcardMatcher { get; }

    /// <summary>
    /// 获取当前请求可用的轻量级追踪上下文。
    /// </summary>
    /// <value>
    /// 追踪上下文实例；当当前宿主未启用 HTTP 追踪能力时返回 <c>null</c>。
    /// </value>
    /// <remarks>
    /// <para>
    /// 业务代码可以通过该入口追加备注、标签与分支说明，用于补充框架自动采集之外的测试线索。
    /// </para>
    /// <para>
    /// 该接口不会暴露框架自动步骤的可变入口，因此不会破坏框架追踪日志的一致性。
    /// </para>
    /// </remarks>
    public abstract IAsgardTraceContext? Trace { get; }
}
