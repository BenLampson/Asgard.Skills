namespace Asgard.Core.Data;

/// <summary>
/// 数据库服务扩展方法，用于注册 FreeSQL 相关服务。
/// </summary>
/// <remarks>
/// <para>提供 FreeSQL 实例的创建和注册功能。</para>
/// <para>支持从配置文件加载数据库配置。</para>
/// </remarks>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// 添加数据库服务，包括 FreeSQL 实例的创建和注册。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="config">数据库配置。</param>
    /// <returns>服务集合实例，支持链式调用。</returns>
    /// <remarks>
    /// <para>此方法会根据配置创建 FreeSQL 实例并注册为单例。</para>
    /// <para>如果配置未启用，则不会注册任何服务。</para>
    /// </remarks>
    public static IServiceCollection AddDatabase(this IServiceCollection services, DatabaseConfig config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        // 验证配置
        config.Validate();

        // 如果未启用，直接返回
        if (!config.Enabled)
        {
            return services;
        }

        // 创建 FreeSQL 实例
        var fsql = CreateFreeSqlInstance(config);

        // 注册 FreeSQL 实例为单益
        _ = services.AddSingleton<IFreeSql>(fsql);

        return services;
    }

    /// <summary>
    /// 根据配置创建 FreeSQL 实例。
    /// </summary>
    /// <param name="config">数据库配置。</param>
    /// <returns>FreeSQL 实例。</returns>
    /// <exception cref="NotSupportedException">当数据库类型不支持时抛出。</exception>
    private static IFreeSql CreateFreeSqlInstance(DatabaseConfig config)
    {
        // 先把配置中的 provider 标识标准化为 FreeSql 的 DataType。
        var dataType = GetDataType(config.Provider);

        // FreeSqlBuilder 只依赖标准化后的数据库类型和连接串，便于后续统一扩展公共选项。
        var fsql = new FreeSqlBuilder()
            .UseConnectionString(dataType, config.ConnectionString)
            .Build();

        return fsql;
    }

    /// <summary>
    /// 将数据库提供者名称转换为 FreeSQL 的 DataType 枚举。
    /// </summary>
    /// <param name="providerName">数据库提供者名称。</param>
    /// <returns>对应的 DataType 枚举值。</returns>
    /// <exception cref="NotSupportedException">当数据库类型不支持时抛出。</exception>
    private static DataType GetDataType(string providerName)
    {
        // 统一转小写比较，兼容配置文件中不同大小写写法。
        return providerName.ToLower() switch
        {
            "sqlserver" => DataType.SqlServer,
            "postgresql" => DataType.PostgreSQL,
            "mysql" => DataType.MySql,
            "sqlite" => DataType.Sqlite,
            "oracle" => DataType.Oracle,
            "dm" => DataType.Dameng,
            "kingbase" => DataType.KingbaseES,
            "人大金仓" => DataType.KingbaseES,
            "达梦" => DataType.Dameng,
            _ => throw new NotSupportedException($"不支持的数据库类型: {providerName}")
        };
    }
}
