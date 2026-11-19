namespace Asgard.Abstractions;

/// <summary>业务使用框架能力的统一短入口。每个能力对象始终存在，可查看其状态、配置及最近异常。</summary>
/// <remarks>控制器使用 AsgardContext，服务可注入本抽象。模块按需解析，整个作用域共用每个能力入口。</remarks>
public abstract class AbsAsgardContext
{
    private FeatureHistory? _history;

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
    public virtual IAsgardMessageQueue MessageQueue => LazyInitializer.EnsureInitialized(ref field,
        () => new MessageQueueFeature(Bind<IMessageQueue>(nameof(MessageQueue))));

    /// <summary>加密服务。</summary>
    public virtual IEncryptionService Encryption => LazyInitializer.EnsureInitialized(ref field,
        () => new EncryptionFeature(Bind<IEncryptionService>(nameof(Encryption))));

    /// <summary>密码哈希。</summary>
    public virtual IPasswordHasher PasswordHasher => LazyInitializer.EnsureInitialized(ref field,
        () => new PasswordHasherFeature(Bind<IPasswordHasher>(nameof(PasswordHasher))));

    /// <summary>密钥生成。</summary>
    public virtual IKeyGenerator KeyGenerator => LazyInitializer.EnsureInitialized(ref field,
        () => new KeyGeneratorFeature(Bind<IKeyGenerator>(nameof(KeyGenerator))));

    /// <summary>查看宿主公开的生效配置并执行校验。</summary>
    public virtual ISystemConfig SystemConfig => LazyInitializer.EnsureInitialized(ref field,
        () => new SystemConfigFeature(Bind<ISystemConfig>(nameof(SystemConfig))));

    /// <summary>通配符匹配。</summary>
    public virtual IWildcardMatcher WildcardMatcher => LazyInitializer.EnsureInitialized(ref field,
        () => new WildcardMatcherFeature(Bind<IWildcardMatcher>(nameof(WildcardMatcher))));

    /// <summary>当前请求追踪及宿主历史追踪查询。</summary>
    public virtual IAsgardTrace Trace => LazyInitializer.EnsureInitialized(ref field,
        () => new TraceFeature(Bind<IAsgardTraceContext>(nameof(Trace), DisabledTraceContext.Instance),
            () => new TraceQueryFeature(Bind<ITraceQueryService>("Trace.Query"))));

    /// <summary>宿主数据库日志查询，调用方负责运维授权。</summary>
    public virtual IDatabaseLogQueryService DatabaseLogQuery => LazyInitializer.EnsureInitialized(ref field,
        () => new DatabaseLogQueryFeature(Bind<IDatabaseLogQueryService>(nameof(DatabaseLogQuery))));
}
