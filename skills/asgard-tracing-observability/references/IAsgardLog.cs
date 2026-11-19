namespace Asgard.Abstractions.Logging;

/// <summary>业务结构化日志和宿主数据库日志查询的统一入口。</summary>
public interface IAsgardLog : ILogger, IAsgardFeature
{
    /// <summary>业务日志写入的装配状态，具体级别是否输出由 <see cref="ILogger.IsEnabled"/> 决定。</summary>
    AsgardFeatureStatus WriteStatus { get; }

    /// <summary>复用当前宿主的日志工厂，按指定分类创建结构化日志。</summary>
    ILogger CreateLogger(string categoryName);

    /// <summary>按标准 .NET 类型分类创建结构化日志。</summary>
    ILogger<TCategory> CreateLogger<TCategory>();

    /// <summary>宿主数据库日志的只读运维查询，调用方负责授权。</summary>
    IDatabaseLogQueryService Query { get; }
}
