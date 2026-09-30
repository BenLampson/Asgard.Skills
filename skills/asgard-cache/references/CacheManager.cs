namespace Asgard.Core.Caching;

/// <summary>管理 Redis 缓存初始化及共享连接生命周期。</summary>
public sealed class CacheManager : ICacheManager
{
    private readonly CacheConfig _config;
    private readonly ILogger<CacheManager> _logger;

    /// <summary>保存配置和日志依赖，连接在初始化阶段创建。</summary>
    public CacheManager(CacheConfig config, ILogger<CacheManager> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);
        _config = config;
        _logger = logger;
    }
    private IAsgardCache? _cache;
    private int _disposed;
    /// <inheritdoc />
    public IAsgardCache Cache => _cache ?? throw new InvalidOperationException("缓存管理器尚未初始化。");
    /// <inheritdoc />
    public bool IsConnected { get; private set; }
    /// <summary>供宿主注册的 Redis 缓存适配器。</summary>
    public IDistributedCache? DistributedCache { get; private set; }
    /// <summary>供分布式锁和 DataProtection 复用的连接。</summary>
    public IConnectionMultiplexer? ConnectionMultiplexer { get; private set; }
    /// <summary>旧本地缓存出口，已无运行行为。</summary>
    [Obsolete("业务本地缓存已移除；认证内部缓存请独立使用 AddMemoryCache。参见 doc/29-缓存迁移-5.3.md；6.0 删除。", true)]
    public IMemoryCache? MemoryCache => null;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
        if (_cache is not null) return;
        _config.Validate();
        if (!_config.Enabled)
        {
            _cache = new NullAsgardCache();
            return;
        }
        try
        {
            var options = RedisCacheConfigurator.CreateRedisCacheOptions(_config);
            ConnectionMultiplexer = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(options.ConfigurationOptions!);
            options.ConnectionMultiplexerFactory = () => Task.FromResult(ConnectionMultiplexer!);
            DistributedCache = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(options);
            // 显式验证连接；启用的 Redis 不可用时终止启动，不静默降级。
            _ = await DistributedCache.GetAsync("__asgard_health_check__", cancellationToken);
            _cache = new RedisAsgardCache(DistributedCache, _config, ConnectionMultiplexer!);
            IsConnected = true;
            _logger.LogDebug("Redis 单层缓存初始化完成。");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        // RedisCache 适配器持有同一连接；先停止适配器，再释放共享连接。
        (DistributedCache as IDisposable)?.Dispose();
        if (ConnectionMultiplexer is not null) await ConnectionMultiplexer.DisposeAsync();
        IsConnected = false;
    }
}
