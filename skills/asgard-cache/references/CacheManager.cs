namespace Asgard.Core.Caching;

/// <summary>
/// 缓存管理器实现，负责缓存系统的初始化、连接验证和生命周期管理。
/// </summary>
/// <remarks>
/// <para>
/// 此类独立于 IoC 容器，在系统启动阶段（Phase 3）完成初始化和连接验证。
/// 初始化时会创建内存缓存和 Redis 连接，并验证 Redis 可用性。
/// </para>
/// </remarks>
public sealed class CacheManager : ICacheManager
{
    private readonly CacheConfig _config;
    private readonly ILogger<CacheManager> _logger;
    private IMemoryCache? _memoryCache;
    private IDistributedCache? _distributedCache;
    private IConnectionMultiplexer? _connectionMultiplexer;
    private MultiLevelCache? _cache;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="CacheManager"/> 类的新实例。
    /// </summary>
    /// <param name="config">缓存配置。</param>
    /// <param name="logger">日志器。</param>
    public CacheManager(CacheConfig config, ILogger<CacheManager> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _config = config;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IMultiLevelCache Cache => _cache ?? throw new InvalidOperationException("缓存管理器尚未初始化，请先调用 InitializeAsync。");

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// 获取内存缓存实例（供 DI 注册使用）。
    /// </summary>
    public IMemoryCache? MemoryCache => _memoryCache;

    /// <summary>
    /// 获取分布式缓存实例（供 DI 注册使用）。
    /// </summary>
    public IDistributedCache? DistributedCache => _distributedCache;

    /// <summary>
    /// 获取 Redis 连接复用器实例（供 DataProtection 等需要直接访问 Redis 的服务使用）。
    /// </summary>
    public IConnectionMultiplexer? ConnectionMultiplexer => _connectionMultiplexer;

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("正在初始化缓存系统...");

        // 先验证配置，避免在创建底层缓存对象后才暴露配置错误。
        _config.Validate();

        // 创建内存缓存
        if (_config.Memory.Enabled)
        {
            _memoryCache = MultiLevelCacheConfigurator.CreateMemoryCache(_config);
            _logger.LogDebug("内存缓存已创建");
        }

        // 创建 Redis 分布式缓存并验证连接
        if (_config.Redis.Enabled)
        {
            var redisOptions = MultiLevelCacheConfigurator.CreateRedisCacheOptions(_config)
                ?? throw new InvalidOperationException("无法创建 Redis 缓存配置");

            // 创建底层 ConnectionMultiplexer，供 DataProtection 等服务复用
            // 先建立底层复用连接，再把它回填给 RedisCache 和后续 DataProtection 等依赖方共享。
            _connectionMultiplexer = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(redisOptions.Configuration!);
            redisOptions.ConnectionMultiplexerFactory = () => Task.FromResult(_connectionMultiplexer);

            var redisCache = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(redisOptions);
            _distributedCache = redisCache;

            // 验证 Redis 连接可用性
            _logger.LogDebug("正在验证 Redis 连接...");
            try
            {
                // 通过一次简单的读操作验证连接
                // 用一次轻量读取验证 Redis 真正可访问，而不只是配置对象构造成功。
                _ = await redisCache.GetAsync("__asgard_health_check__", cancellationToken);
                _logger.LogDebug("Redis 连接验证成功");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Redis 连接验证失败: {ex.Message}", ex);
            }
        }

        // 创建多级缓存实例
        // 无论启用哪些底层缓存，最终都统一包装成 MultiLevelCache 暴露给上层使用。
        _cache = new MultiLevelCache(_memoryCache, _distributedCache, _config);
        IsConnected = true;

        _logger.LogDebug("缓存系统初始化完成");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // 先释放分布式缓存实例，再释放底层连接和内存缓存，避免对象之间还存在未完成引用。
        if (_distributedCache is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_distributedCache is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_connectionMultiplexer is not null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }

        _memoryCache?.Dispose();
        IsConnected = false;
    }
}
