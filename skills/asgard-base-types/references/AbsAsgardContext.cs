namespace Asgard.Abstractions;

/// <summary>业务使用框架能力的统一短入口。每个能力对象始终存在，可查看其状态、配置及最近异常。</summary>
/// <remarks>控制器使用 AsgardContext，服务可注入本抽象。模块按需解析，整个作用域共用每个能力入口。</remarks>
public abstract class AbsAsgardContext
{
    private FeatureHistory? _history;
    private IReadOnlyDictionary<string, IAsgardFeature>? _features;

    /// <summary>自定义上下文继承到的默认能力为关闭状态。</summary>
    internal virtual FeatureBinding<T> Bind<T>(string name, T? fallback = null) where T : class =>
        new(new FeatureDescriptor(name, false, history: LazyInitializer.EnsureInitialized(ref _history)), () => null, fallback);

    /// <summary>派生宿主可在这里接入已注册的 Redis 对象。</summary>
    internal virtual IAsgardRedis CreateRedis() => new AsgardRedisFeature(Bind<IAsgardRedis>(nameof(Redis)));

    /// <summary>Redis、对象缓存、消息、锁与 Pipeline 的统一入口。</summary>
    public virtual IAsgardRedis Redis => LazyInitializer.EnsureInitialized(ref field, CreateRedis);

    /// <summary>业务数据库和工作单元，沿用当前租户作用域。</summary>
    public virtual IAsgardDatabase Database => LazyInitializer.EnsureInitialized(ref field,
        () => new DatabaseFeature(Bind<IFreeSql>(nameof(Database))));

    /// <summary>兼容入口，与 Redis.Cache 共用同一能力对象。</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public virtual IAsgardCache Cache => Redis.Cache;

    /// <summary>兼容入口，与 Redis.PubSub 共用同一能力对象。</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public virtual IRedisPubSub RedisPubSub => Redis.PubSub;

    /// <summary>兼容入口，与 Redis.Stream 共用同一能力对象。</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public virtual IRedisStream RedisStream => Redis.Stream;

    /// <summary>兼容入口，与 Redis.Locks 共用同一能力对象。</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public virtual IDistributedLock DistributedLock => Redis.Locks;

    /// <summary>压缩和解压。</summary>
    public virtual ICompressionService Compression => LazyInitializer.EnsureInitialized(ref field,
        () => new CompressionFeature(Bind<ICompressionService>(nameof(Compression))));

    /// <summary>租户作用域。</summary>
    public virtual ITenantScopeFactory TenantScopeFactory => LazyInitializer.EnsureInitialized(ref field,
        () => new TenantScopeFeature(Bind<ITenantScopeFactory>(nameof(TenantScopeFactory))));

    /// <summary>当前身份。</summary>
    public virtual IAsgardIdentityContext IdentityContext => LazyInitializer.EnsureInitialized(ref field,
        () => new IdentityFeature(Bind<IAsgardIdentityContext>(nameof(IdentityContext))));

    /// <summary>任务调度。</summary>
    public virtual IAsgardJobScheduler JobScheduler => LazyInitializer.EnsureInitialized(ref field,
        () => new JobSchedulerFeature(Bind<IJobScheduler>(nameof(JobScheduler))));

    /// <summary>消息队列。</summary>
    public virtual IAsgardMessageQueue MessageQueue => LazyInitializer.EnsureInitialized(ref field, CreateMessageQueue);

    /// <summary>让宿主从当前作用域装配消息队列及其类型化子能力。</summary>
    internal virtual IAsgardMessageQueue CreateMessageQueue() => new MessageQueueFeature(Bind<IMessageQueue>(nameof(MessageQueue)));

    /// <summary>加密服务。</summary>
    public virtual IEncryptionService Encryption => LazyInitializer.EnsureInitialized(ref field,
        () => new EncryptionFeature(Bind<IEncryptionService>(nameof(Encryption))));

    /// <summary>密码哈希。</summary>
    public virtual IPasswordHasher PasswordHasher => LazyInitializer.EnsureInitialized(ref field,
        () => new PasswordHasherFeature(Bind<IPasswordHasher>(nameof(PasswordHasher))));

    /// <summary>密钥生成。</summary>
    public virtual IKeyGenerator KeyGenerator => LazyInitializer.EnsureInitialized(ref field,
        () => new KeyGeneratorFeature(Bind<IKeyGenerator>(nameof(KeyGenerator))));

    /// <summary>查看宿主公开的生效配置并执行校验，保留既有属性签名。</summary>
    public virtual ISystemConfig SystemConfig => LazyInitializer.EnsureInitialized(ref field,
        CreateConfig);

    /// <summary>读取当前宿主和插件注册的强类型配置。兼容自定义上下文重写的 SystemConfig。</summary>
    public virtual IAsgardConfig Config => LazyInitializer.EnsureInitialized(ref field, () =>
    {
        var existing = SystemConfig;
        if (existing is IAsgardConfig reader) return reader;
        var descriptor = new FeatureDescriptor(nameof(Config), true, history: LazyInitializer.EnsureInitialized(ref _history));
        descriptor.Attach(existing);
        return new SystemConfigFeature(new(descriptor, () => existing), ResolveConfiguration);
    });

    /// <summary>按需装配校验和强类型配置的共同入口。</summary>
    internal virtual IAsgardConfig CreateConfig() =>
        new SystemConfigFeature(Bind<ISystemConfig>(nameof(Config)), ResolveConfiguration);

    /// <summary>从当前作用域解析已注册的业务配置。</summary>
    internal virtual ISystemConfig? ResolveConfiguration(Type configType) => null;

    /// <summary>列出框架的规范顶层入口。通过接口序列化只会展示状态、脱敏配置和最近异常，别名不会重复出现。</summary>
    public virtual IReadOnlyDictionary<string, IAsgardFeature> GetFeatures() => LazyInitializer.EnsureInitialized(ref _features,
        () => new Dictionary<string, IAsgardFeature>(StringComparer.Ordinal)
        {
            [nameof(Redis)] = Redis,
            [nameof(Database)] = Database,
            [nameof(MessageQueue)] = MessageQueue,
            [nameof(JobScheduler)] = JobScheduler,
            [nameof(IdentityContext)] = IdentityContext,
            [nameof(TenantScopeFactory)] = TenantScopeFactory,
            [nameof(Trace)] = Trace,
            [nameof(Log)] = Log,
            [nameof(Config)] = Config,
            [nameof(Encryption)] = Encryption,
            [nameof(PasswordHasher)] = PasswordHasher,
            [nameof(KeyGenerator)] = KeyGenerator,
            [nameof(Compression)] = Compression,
            [nameof(WildcardMatcher)] = WildcardMatcher
        }.AsReadOnly());

    /// <summary>通配符匹配。</summary>
    public virtual IWildcardMatcher WildcardMatcher => LazyInitializer.EnsureInitialized(ref field,
        () => new WildcardMatcherFeature(Bind<IWildcardMatcher>(nameof(WildcardMatcher))));

    /// <summary>当前请求追踪及宿主历史追踪查询。</summary>
    public virtual IAsgardTrace Trace => LazyInitializer.EnsureInitialized(ref field,
        () => new TraceFeature(Bind<IAsgardTraceContext>(nameof(Trace), DisabledTraceContext.Instance),
            () => new TraceQueryFeature(Bind<ITraceQueryService>("Trace.Query"))));

    /// <summary>业务结构化日志，兼容标准 ILogger 的级别、分类和作用域。</summary>
    public virtual Logging.IAsgardLog Log => LazyInitializer.EnsureInitialized(ref field,
        () => new LogFeature(Bind<ILoggerFactory>(nameof(Log), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance),
            () => DatabaseLogQuery));

    /// <summary>兼容入口，与 Log.Query 共用同一数据库日志查询服务。</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public virtual IDatabaseLogQueryService DatabaseLogQuery => LazyInitializer.EnsureInitialized(ref field,
        () => new DatabaseLogQueryFeature(Bind<IDatabaseLogQueryService>("Log.Query")));
}
