namespace Asgard.Abstractions.Caching;

/// <summary>业务代码通过 AsgardContext.Redis 使用缓存、消息、锁、Pipeline 及原生命令。</summary>
public interface IAsgardRedis : IAsgardFeature
{
    /// <summary>Redis 命令、原生数据库、事务及 Pipeline 的装配状态。</summary>
    AsgardFeatureStatus CommandStatus => Status == AsgardFeatureStatus.Enabled ? AsgardFeatureStatus.Enabled : AsgardFeatureStatus.Disabled;

    /// <summary>类型化对象缓存，支持批量读写和同键回源。</summary>
    IAsgardCache Cache { get; }

    /// <summary>框架频道的消息发布与订阅。</summary>
    IRedisPubSub PubSub { get; }

    /// <summary>框架 Stream 消息及消费组操作。</summary>
    IRedisStream Stream { get; }

    /// <summary>分布式锁，可使用显式重入与围栏令牌。</summary>
    IDistributedLock Locks { get; }

    /// <summary>在业务工作单元内登记缓存失效，提交后按配置投递。</summary>
    void EnqueueInvalidation(IUnitOfWork unit, IEnumerable<string> keys, IEnumerable<string> prefixes);

    /// <summary>带应用业务数据前缀的原生客户端。</summary>
    IDatabase Data { get; }

    /// <summary>使用 Stream 配置库和前缀的原生客户端。</summary>
    IDatabase Streams { get; }

    /// <summary>同步排入命令，统一发送并等待所有响应；调用方可保留各个结果任务。</summary>
    Task PipelineAsync(Func<IBatch, IReadOnlyCollection<Task>> enqueue, CancellationToken token = default);

    /// <summary>执行带条件的 Redis 事务，条件失败返回 false，命令错误传播给调用方。</summary>
    Task<bool> TransactionAsync(Func<ITransaction, IReadOnlyCollection<Task>> enqueue, CancellationToken token = default);

    /// <summary>执行调用方的 Lua 脚本，KEYS 自动进入业务数据命名空间。</summary>
    Task<RedisResult> EvaluateAsync(string script, IReadOnlyList<string> keys, params RedisValue[] arguments);

    /// <summary>写入原生 String，可指定期限及存在性条件。</summary>
    Task<bool> StringSetAsync(string key, RedisValue value, TimeSpan? expiration = null, When when = When.Always);

    /// <summary>读取原生 String，与对象缓存的 JSON 编码相互独立。</summary>
    Task<RedisValue> StringGetAsync(string key);

    /// <summary>原子增加或减少计数。</summary>
    Task<long> IncrementAsync(string key, long delta = 1);

    /// <summary>设置一个 Hash 字段。</summary>
    Task<bool> HashSetAsync(string key, string field, RedisValue value);

    /// <summary>读取一个 Hash 字段。</summary>
    Task<RedisValue> HashGetAsync(string key, string field);

    /// <summary>添加集合成员，返回是否新增。</summary>
    Task<bool> SetAddAsync(string key, RedisValue member);

    /// <summary>删除集合成员。</summary>
    Task<bool> SetRemoveAsync(string key, RedisValue member);

    /// <summary>读取集合中的成员。</summary>
    Task<RedisValue[]> SetMembersAsync(string key);

    /// <summary>设置成员排名分数。</summary>
    Task<bool> SortedSetAddAsync(string key, RedisValue member, double score);

    /// <summary>按分数从高到低读出排名区间。</summary>
    Task<SortedSetEntry[]> RankAsync(string key, long start, long stop);

    /// <summary>将一个值追加到列表尾部，返回列表长度。</summary>
    Task<long> ListRightPushAsync(string key, RedisValue value);

    /// <summary>取出列表头部的一个值，适用于先进先出队列。</summary>
    Task<RedisValue> ListLeftPopAsync(string key);

    /// <summary>按下标读取列表，结束下标包含在结果中。</summary>
    Task<RedisValue[]> ListRangeAsync(string key, long start, long stop);

    /// <summary>设置位图的一位，返回该位原来的值。</summary>
    Task<bool> SetBitAsync(string key, long offset, bool value);

    /// <summary>读取位图中的一位。</summary>
    Task<bool> GetBitAsync(string key, long offset);

    /// <summary>添加地理位置，坐标按经度、纬度传入。</summary>
    Task<bool> GeoAddAsync(string key, double longitude, double latitude, RedisValue member);

    /// <summary>计算两个已登记成员的距离，默认单位为米。</summary>
    Task<double?> GeoDistanceAsync(string key, RedisValue first, RedisValue second, GeoUnit unit = GeoUnit.Meters);

    /// <summary>向 HyperLogLog 添加成员，返回底层估算状态是否发生变化。</summary>
    Task<bool> HyperLogLogAddAsync(string key, RedisValue member);

    /// <summary>估算 HyperLogLog 中不同成员的数量。</summary>
    Task<long> HyperLogLogLengthAsync(string key);

    /// <summary>查询业务数据键的剩余期限。</summary>
    Task<TimeSpan?> TimeToLiveAsync(string key);

    /// <summary>单独设置业务数据键的期限。</summary>
    Task<bool> ExpireAsync(string key, TimeSpan expiration);

    /// <summary>查询 IAsgardCache 对象键的 Redis 期限。</summary>
    Task<TimeSpan?> CacheTimeToLiveAsync(string key);

    /// <summary>原子更新对象缓存的绝对过期元数据和实际毫秒期限。</summary>
    Task<bool> ExpireCacheAsync(string key, TimeSpan expiration);

    /// <summary>按模式订阅框架 Pub/Sub 频道。返回的队列包含实际频道，调用者负责退订。</summary>
    Task<ChannelMessageQueue> SubscribePatternAsync(string pattern);

    /// <summary>以严格递增的围栏更新 Redis Hash 中的值；旧序号或重复提交返回 false。</summary>
    Task<bool> SetFencedAsync(string key, long fence, RedisValue value);
}
