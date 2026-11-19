namespace Asgard.Abstractions.Data;

/// <summary>业务数据库的工作单元入口。复用宿主的 FreeSql 实例和租户规则。</summary>
public interface IAsgardDatabase : IAsgardFeature
{
    /// <summary>借用宿主的原生 FreeSql 进行查询或命令，事务内应使用对应的 unit.Orm。</summary>
    /// <remarks>旧版自定义数据库门面可继续只实现工作单元操作。</remarks>
    [JsonIgnore]
    IFreeSql Orm => Status == AsgardFeatureStatus.Disabled
        ? throw new AsgardFeatureDisabledException("Database") : throw new NotSupportedException("此数据库实现只提供工作单元。");

    /// <summary>创建独立工作单元，由调用者提交或回滚并释放。</summary>
    IUnitOfWork CreateUnitOfWork();

    /// <summary>在同一工作单元内执行操作，成功后提交，失败或提交前取消时回滚。</summary>
    Task InTransactionAsync(Func<IUnitOfWork, CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
