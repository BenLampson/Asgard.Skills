namespace Asgard.Abstractions.Caching;



/// <summary>
/// 缓存配置类，用于配置多级缓存。
/// </summary>
/// <remarks>
/// <para>
/// 此类实现 <see cref="ISystemConfig"/> 接口，与 Asgard 的配置体系集成。
/// 支持配置内存缓存（一级缓存）和 Redis 分布式缓存（二级缓存）。
/// 多级缓存策略：获取时先查内存缓存，未命中再查 Redis；写入时同时写入二级缓存。
/// </para>
/// </remarks>
/// <example>
/// 配置示例（单独文件）：
/// <code>
/// Enabled: true
/// Memory:
///   Enabled: true
///   DefaultExpirationMinutes: 5
///   SizeLimit: 104857600
/// Redis:
///   Enabled: true
///   ConnectionString: "localhost:6379"
///   InstanceName: "Asgard:"
///   DefaultExpirationMinutes: 30
/// </code>
/// 
/// 配置示例（合并文件）：
/// <code>
/// caching:
///   enabled: true
///   memory:
///     enabled: true
///     defaultExpirationMinutes: 5
///     sizeLimit: 104857600
///   redis:
///     enabled: true
///     connectionString: "localhost:6379"
///     instanceName: "Asgard:"
///     defaultExpirationMinutes: 30
/// </code>
/// </example>
public class CacheConfig : ISystemConfig
{
    /// <summary>
    /// 获取或设置是否启用缓存模块。
    /// </summary>
    /// <remarks>
    /// 当设置为 false 时，整个缓存模块将被禁用，不会注册任何缓存服务。
    /// 默认值为 false。
    /// </remarks>
    /// <value>true 表示启用缓存模块；false 表示禁用。</value>
    [ConfigPath("caching.enabled", DefaultValue = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 获取或设置内存缓存配置。
    /// </summary>
    /// <remarks>
    /// 内存缓存作为一级缓存，提供最低的访问延迟。
    /// 适合存储热点数据，减少对分布式缓存的访问压力。
    /// </remarks>
    /// <value>内存缓存配置对象。</value>
    [ConfigPath("caching.memory")]
    public MemoryCacheOptions Memory { get; set; } = new();

    /// <summary>
    /// 获取或设置 Redis 缓存配置。
    /// </summary>
    /// <remarks>
    /// Redis 缓存作为二级缓存，提供分布式环境下的数据共享能力。
    /// 支持多实例部署环境下的缓存一致性。
    /// </remarks>
    /// <value>Redis 缓存配置对象。</value>
    [ConfigPath("caching.redis")]
    public RedisCacheOptions Redis { get; set; } = new();

    /// <summary>
    /// 验证配置的有效性。
    /// </summary>
    /// <remarks>
    /// 此方法在配置加载完成后自动调用。验证以下内容：
    /// <list type="number">
    ///   <item><description>Memory 和 Redis 配置对象不为 null</description></item>
    ///   <item><description>内存缓存过期时间必须大于 0</description></item>
    ///   <item><description>Redis 启用时，连接字符串不能为空</description></item>
    ///   <item><description>Redis 过期时间必须大于 0</description></item>
    ///   <item><description>超时时间必须大于 0</description></item>
    ///   <item><description>至少启用一种缓存</description></item>
    /// </list>
    /// 验证失败时将抛出 <see cref="InvalidOperationException"/> 异常。
    /// </remarks>
    /// <exception cref="InvalidOperationException">当配置无效时抛出。</exception>
    public void Validate()
    {
        if (Memory is null)
        {
            throw new InvalidOperationException("Memory config cannot be null.");
        }

        if (Redis is null)
        {
            throw new InvalidOperationException("Redis config cannot be null.");
        }

        if (Memory.Enabled)
        {
            if (Memory.DefaultExpirationMinutes <= 0)
            {
                throw new InvalidOperationException("Memory default expiration must be greater than 0.");
            }

            if (Memory.SizeLimit.HasValue && Memory.SizeLimit.Value <= 0)
            {
                throw new InvalidOperationException("Memory size limit must be greater than 0.");
            }

            if (Memory.CompactOnMemoryPressure is <= 0 or > 1)
            {
                throw new InvalidOperationException("Memory compact threshold must be between 0 and 1.");
            }
        }

        if (Redis.Enabled)
        {
            if (string.IsNullOrWhiteSpace(Redis.ConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is required when Redis is enabled.");
            }

            if (Redis.DefaultExpirationMinutes <= 0)
            {
                throw new InvalidOperationException("Redis default expiration must be greater than 0.");
            }

            if (Redis.ConnectTimeout <= 0)
            {
                throw new InvalidOperationException("Redis connect timeout must be greater than 0.");
            }

            if (Redis.SyncTimeout <= 0)
            {
                throw new InvalidOperationException("Redis sync timeout must be greater than 0.");
            }

            if (Redis.AsyncTimeout <= 0)
            {
                throw new InvalidOperationException("Redis async timeout must be greater than 0.");
            }

            if (Redis.Database is < 0 or > 15)
            {
                throw new InvalidOperationException("Redis database index must be between 0 and 15.");
            }

            if (Redis.RetryCount < 0)
            {
                throw new InvalidOperationException("Redis retry count cannot be negative.");
            }

            if (Redis.RetryIntervalMilliseconds <= 0)
            {
                throw new InvalidOperationException("Redis retry interval must be greater than 0.");
            }
        }

        // 只有当启用了缓存模块时，才要求至少启用一种缓存提供者
        if (Enabled && !Memory.Enabled && !Redis.Enabled)
        {
            throw new InvalidOperationException("At least one cache provider must be enabled.");
        }
    }
}

