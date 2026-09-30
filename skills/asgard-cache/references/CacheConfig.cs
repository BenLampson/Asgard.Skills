namespace Asgard.Abstractions.Caching;

/// <summary>业务缓存配置；5.3 起仅支持 Redis。</summary>
public class CacheConfig : ISystemConfig
{
    /// <summary>是否启用 Redis 业务缓存；关闭时不建立连接。</summary>
    [ConfigPath("caching.enabled", DefaultValue = false)]
    public bool Enabled { get; set; }

    /// <summary>旧本地缓存配置，仅保留迁移诊断声明。</summary>
    [Obsolete("删除 Memory / caching.memory，改用 caching.redis 配置 Redis；参见 doc/29-缓存迁移-5.3.md。6.0 删除。", true)]
    [ConfigPath("caching.memory")]
    public MemoryCacheOptions Memory { get; set; } = new();

    /// <summary>Redis 连接、命名空间和默认 TTL。</summary>
    [ConfigPath("caching.redis")]
    public RedisCacheOptions Redis { get; set; } = new();

    /// <summary>启用时验证 Redis 配置，关闭时不要求有效的连接信息。</summary>
    public void Validate()
    {
        if (!Enabled) return;
        ArgumentNullException.ThrowIfNull(Redis);
        if (string.IsNullOrWhiteSpace(Redis.ConnectionString))
            throw new InvalidOperationException("caching.redis.connectionString 不能为空。");
        if (Redis.DefaultExpirationMinutes <= 0)
            throw new InvalidOperationException("Redis default expiration must be greater than 0.");
        if (Redis.ConnectTimeout <= 0 || Redis.SyncTimeout <= 0 || Redis.AsyncTimeout <= 0)
            throw new InvalidOperationException("Redis timeouts must be greater than 0.");
        if (Redis.Database is < 0 or > 15)
            throw new InvalidOperationException("Redis database index must be between 0 and 15.");
        if (Redis.RetryCount < 0 || Redis.RetryIntervalMilliseconds <= 0)
            throw new InvalidOperationException("Redis retry configuration is invalid.");
    }
}
