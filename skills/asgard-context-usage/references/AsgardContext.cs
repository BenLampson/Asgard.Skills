namespace Asgard.Core;

/// <summary>从当前 DI 作用域按需装配业务能力，每个入口均可在没有基础设施时读取诊断。</summary>
public class AsgardContext : AbsAsgardContext
{
    private readonly IServiceProvider? _services;
    private readonly IServiceProviderIsService? _registered;
    private readonly FeatureRegistrations? _registrations;
    private Dictionary<Type, object>? _manual;
    private readonly FeatureHistory _history;

    /// <summary>保留手动装配的原有构造签名，缺省能力提供关闭状态的对象。</summary>
    public AsgardContext(
        IAsgardCache? cache = null,
        ICompressionService? compression = null,
        ITenantScopeFactory? tenantScopeFactory = null,
        IAsgardIdentityContext? identityContext = null,
        IJobScheduler? jobScheduler = null,
        IMessageQueue? messageQueue = null,
        IDistributedLock? distributedLock = null,
        IEncryptionService? encryption = null,
        IPasswordHasher? passwordHasher = null,
        IKeyGenerator? keyGenerator = null,
        ISystemConfig? systemConfig = null,
        IWildcardMatcher? wildcardMatcher = null,
        IAsgardTraceContext? trace = null)
    {
        _history = new();
        Add(cache);
        Add(compression);
        Add(tenantScopeFactory);
        Add(identityContext);
        Add(jobScheduler);
        Add(messageQueue);
        Add(distributedLock);
        Add(encryption);
        Add(passwordHasher);
        Add(keyGenerator);
        Add(systemConfig);
        Add(wildcardMatcher);
        Add(trace);
    }

    /// <summary>仅持有当前作用域及诊断，不在创建 Context 时构造业务依赖。</summary>
    internal AsgardContext(IServiceProvider provider, FeatureHistory history)
    {
        _services = provider;
        _registered = provider.GetRequiredService<IServiceProviderIsService>();
        _registrations = provider.GetRequiredService<FeatureRegistrations>();
        _history = history;
    }

    private void Add<T>(T? service) where T : class
    {
        if (service is not null) (_manual ??= [])[typeof(T)] = service;
    }

    /// <inheritdoc />
    internal override ISystemConfig? ResolveConfiguration(Type configType) =>
        _services?.GetService(configType) as ISystemConfig;

    /// <summary>完整宿主复用宿主校验器；只注册具体配置的自定义容器也能按类型读取。</summary>
    internal override IAsgardConfig CreateConfig()
    {
        if (_services is null) return base.CreateConfig();
        if (_registered!.IsService(typeof(IAsgardConfig)))
        {
            var reader = Bind<IAsgardConfig>(nameof(Config));
            return new SystemConfigFeature(new(reader.Descriptor, () => reader.Service), ResolveConfiguration);
        }
        if (_registered.IsService(typeof(ISystemConfig))) return base.CreateConfig();
        var types = _registrations!.ConfigurationTypes();
        if (types.Length == 0) return base.CreateConfig();
        var descriptor = FeatureRegistrations.Describe(_services, nameof(Config));
        return new SystemConfigFeature(new(descriptor, () => new RegisteredConfiguration(_services, types)), ResolveConfiguration);
    }

    /// <summary>结合配置与容器的注册状态，实际服务在第一次业务调用时解析。</summary>
    internal override FeatureBinding<T> Bind<T>(string name, T? fallback = null) where T : class
    {
        var (allowed, values) = FeatureConfiguration.Read(_services, name);
        object? instance = null;
        var present = _manual?.TryGetValue(typeof(T), out instance) == true || _registered?.IsService(typeof(T)) == true;
        // 能力诊断查询不会启动日志工厂。写日志本身只记录提供方异常，避免再向同一提供方写入日志。
        var descriptor = new FeatureDescriptor(name, allowed && present, values, _history, resolveLogger:
            name == "Log" || _registered?.IsService(typeof(ILoggerFactory)) != true ? null :
            () => _services!.GetRequiredService<ILoggerFactory>().CreateLogger($"Asgard.Features.{name.Split('.')[0]}"));
        descriptor.Attach(instance as IAsgardFeature ?? _registrations?.Declaration<T>());
        return new(descriptor, () => (T?)instance ?? _services?.GetService<T>(), fallback);
    }

    /// <summary>类型化消息助手和自定义重试策略始终在持有当前 Context 的作用域中解析。</summary>
    internal override IAsgardMessageQueue CreateMessageQueue() => new MessageQueueFeature(Bind<IMessageQueue>(nameof(MessageQueue)),
        new MessageQueueHelpers(_services, _registrations));

    /// <summary>默认 Redis 直接复用宿主提供的同一门面。单独装配的子服务也能从同一入口访问。</summary>
    internal override IAsgardRedis CreateRedis()
    {
        if (_registered?.IsService(typeof(AsgardRedis)) == true)
            return _services!.GetRequiredService<AsgardRedis>();

        var binding = Bind<IAsgardRedis>(nameof(Redis));
        if (_registered?.IsService(typeof(IAsgardRedis)) == true) return new AsgardRedis(binding);

        return new AsgardRedis(binding, Bind<IAsgardCache>("Redis.Cache", new DisabledAsgardCache()),
            Bind<IRedisPubSub>("Redis.PubSub"), Bind<IRedisStream>("Redis.Stream"), Bind<IDistributedLock>("Redis.Locks"));
    }
}
