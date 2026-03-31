namespace Asgard.Core;

/// <summary>
/// Asgard 框架统一上下文实现类，提供所有模块的统一访问入口。
/// </summary>
/// <remarks>
/// <para>
/// 此类继承 <see cref="AbsAsgardContext"/>, 通过构造函数注入所有模块服务。
/// 未注册的服务将为 <c>null</c>, 用户可优雅降级处理。
/// </para>
/// <para>
/// <b>生命周期：</b>
/// 注册为 Scoped 生命周期，每次请求创建新实例。
/// </para>
/// <para>
/// <b>使用示例：</b>
/// <code>
/// // 注册服务
/// services.AddAsgardContext();
/// 
/// // 使用
/// public class MyService
/// {
///     private readonly AbsAsgardContext _context;
///     
///     public MyService(AbsAsgardContext context)
///     {
///         _context = context;
///     }
///     
///     public void DoSomething()
///     {
///         // 访问缓存
///         _context.Cache?.GetAsync<string>("key");
///         
///         // 访问消息队列
///         _context.MessageQueue?.PublishAsync("topic", message);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public class AsgardContext : AbsAsgardContext
{
    private readonly IMultiLevelCache? _cache;
    private readonly ICompressionService? _compression;
    private readonly ITenantScopeFactory? _tenantScopeFactory;
    private readonly IAsgardIdentityContext? _identityContext;
    private readonly IJobScheduler? _jobScheduler;
    private readonly IMessageQueue? _messageQueue;
    private readonly IEncryptionService? _encryption;
    private readonly IPasswordHasher? _passwordHasher;
    private readonly IKeyGenerator? _keyGenerator;
    private readonly ISystemConfig? _systemConfig;
    private readonly IWildcardMatcher? _wildcardMatcher;

    /// <summary>
    /// 初始化 <see cref="AsgardContext"/> 类的新实例。
    /// </summary>
    /// <param name="cache">多级缓存服务（可选）。</param>
    /// <param name="compression">压缩服务（可选）。</param>
    /// <param name="tenantScopeFactory">租户作用域工厂（可选）。</param>
    /// <param name="identityContext">身份上下文（可选）。</param>
    /// <param name="jobScheduler">任务调度器（可选）。</param>
    /// <param name="messageQueue">消息队列（可选）。</param>
    /// <param name="encryption">加密服务（可选）。</param>
    /// <param name="passwordHasher">密码哈希器（可选）。</param>
    /// <param name="keyGenerator">密钥生成器（可选）。</param>
    /// <param name="systemConfig">系统配置（可选）。</param>
    /// <param name="wildcardMatcher">通配符匹配器（可选）。</param>
    /// <remarks>
    /// <para>
    /// 所有参数均为可选，未注册的服务将为 <c>null</c>。
    /// 这允许用户根据需要选择性注册模块，无需注册所有服务。
    /// </para>
    /// <para>
    /// <b>依赖注入示例：</b>
    /// <code>
    /// // 只注册需要的服务
    /// services.AddMultiLevelCache();
    /// services.AddAsgardContext();
    /// 
    /// // Context.Cache 有值，其他属性为 null
    /// </code>
    /// </para>
    /// </remarks>
    public AsgardContext(
        IMultiLevelCache? cache = null,
        ICompressionService? compression = null,
        ITenantScopeFactory? tenantScopeFactory = null,
        IAsgardIdentityContext? identityContext = null,
        IJobScheduler? jobScheduler = null,
        IMessageQueue? messageQueue = null,
        IEncryptionService? encryption = null,
        IPasswordHasher? passwordHasher = null,
        IKeyGenerator? keyGenerator = null,
        ISystemConfig? systemConfig = null,
        IWildcardMatcher? wildcardMatcher = null)
    {
        _cache = cache;
        _compression = compression;
        _tenantScopeFactory = tenantScopeFactory;
        _identityContext = identityContext;
        _jobScheduler = jobScheduler;
        _messageQueue = messageQueue;
        _encryption = encryption;
        _passwordHasher = passwordHasher;
        _keyGenerator = keyGenerator;
        _systemConfig = systemConfig;
        _wildcardMatcher = wildcardMatcher;
    }

    /// <inheritdoc />
    public override IMultiLevelCache? Cache => _cache;

    /// <inheritdoc />
    public override ICompressionService? Compression => _compression;

    /// <inheritdoc />
    public override ITenantScopeFactory? TenantScopeFactory => _tenantScopeFactory;

    /// <inheritdoc />
    public override IAsgardIdentityContext? IdentityContext => _identityContext;

    /// <inheritdoc />
    public override IJobScheduler? JobScheduler => _jobScheduler;

    /// <inheritdoc />
    public override IMessageQueue? MessageQueue => _messageQueue;

    /// <inheritdoc />
    public override IEncryptionService? Encryption => _encryption;

    /// <inheritdoc />
    public override IPasswordHasher? PasswordHasher => _passwordHasher;

    /// <inheritdoc />
    public override IKeyGenerator? KeyGenerator => _keyGenerator;

    /// <inheritdoc />
    public override ISystemConfig? SystemConfig => _systemConfig;

    /// <inheritdoc />
    public override IWildcardMatcher? WildcardMatcher => _wildcardMatcher;
}
