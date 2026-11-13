namespace Asgard.Core.Caching;

/// <summary>管理 Redis 缓存初始化及共享连接生命周期。</summary>
public sealed class CacheManager : ICacheManager
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly CacheConfig _config;
    private readonly ILogger<CacheManager> _logger;
    private readonly Func<ConfigurationOptions, Task<IConnectionMultiplexer>> _connectAsync;

    /// <summary>保存配置和日志依赖，连接在初始化阶段创建。</summary>
    public CacheManager(CacheConfig config, ILogger<CacheManager> logger)
        : this(config, logger, async options => await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(options))
    {
    }

    /// <summary>测试入口：连接工厂不改变公开构造或默认行为。</summary>
    internal CacheManager(CacheConfig config, ILogger<CacheManager> logger,
        Func<ConfigurationOptions, Task<IConnectionMultiplexer>> connectAsync)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);
        _config = config;
        _logger = logger;
        _connectAsync = connectAsync;
    }
    private IAsgardCache? _cache;
    private int _disposed;
    private bool _adapterOwnsConnection;
    /// <inheritdoc />
    public IAsgardCache Cache => _cache ?? throw new InvalidOperationException("缓存管理器尚未初始化。");
    /// <inheritdoc />
    public bool IsConnected { get; private set; }
    /// <summary>供宿主注册的 Redis 缓存适配器。</summary>
    public IDistributedCache? DistributedCache { get; private set; }
    /// <summary>供分布式锁和 DataProtection 复用的连接。</summary>
    public IConnectionMultiplexer? ConnectionMultiplexer { get; private set; }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
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
                ConnectionMultiplexer = await _connectAsync(options.ConfigurationOptions!);
                options.ConnectionMultiplexerFactory = () => Task.FromResult(
                    RedisConnectionOwnership.Track(ConnectionMultiplexer!, () => _adapterOwnsConnection = true));
                DistributedCache = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(options);
                // 显式验证连接；启用的 Redis 不可用时终止启动，不静默降级。
                _ = await DistributedCache.GetAsync("__asgard_health_check__", cancellationToken);
                _cache = new RedisAsgardCache(DistributedCache, _config, ConnectionMultiplexer!);
                IsConnected = true;
                _logger.LogDebug("Redis 单层缓存初始化完成。");
            }
            catch (Exception initializationError)
            {
                try { await DisposeCoreAsync(); }
                catch (Exception cleanupError) { throw new AggregateException(initializationError, cleanupError); }
                throw;
            }
        }
        finally { _ = _lifecycleGate.Release(); }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _lifecycleGate.WaitAsync();
        try { await DisposeCoreAsync(); }
        finally { _ = _lifecycleGate.Release(); }
    }

    private async ValueTask DisposeCoreAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        var failures = new List<Exception>();
        var releaseConnection = !_adapterOwnsConnection;
        try { (DistributedCache as IDisposable)?.Dispose(); }
        catch (Exception ex)
        {
            failures.Add(ex);
            releaseConnection = true;
        }
        try
        {
            if (releaseConnection && ConnectionMultiplexer is not null)
                await ConnectionMultiplexer.DisposeAsync();
        }
        catch (Exception ex) { failures.Add(ex); }
        finally { IsConnected = false; }
        if (failures.Count > 0) throw new AggregateException("释放缓存资源失败。", failures);
    }
}
