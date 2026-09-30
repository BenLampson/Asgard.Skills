## 代码示例

### 作业实现

```csharp
namespace {Namespace}.Jobs;

/// <summary>
/// {JobSummary}
/// </summary>
public class {JobName} : IJob
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asgardContext">Asgard 上下文</param>
    public {JobName}(AbsAsgardContext asgardContext)
    {
        AsgardContext = asgardContext;
    }

    /// <summary>
    /// Asgard 上下文
    /// </summary>
    protected AbsAsgardContext AsgardContext { get; }

    /// <summary>
    /// 执行作业
    /// </summary>
    /// <param name="context">作业执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        {ExecuteBody}
    }
}
```

### 插件动态注册

```csharp
/// <summary>
/// 初始化完成后动态注册作业
/// </summary>
/// <param name="cancellationToken">取消令牌</param>
public override async Task InitializeAsync(CancellationToken cancellationToken)
{
    await base.InitializeAsync(cancellationToken);

    if (AsgardContext.JobScheduler != null)
    {
        await AsgardContext.JobScheduler.ScheduleJobAsync<{JobName}>(
            new JobKey("{JobKey}"),
            trigger =>
            {
                trigger.WithCronSchedule("{CronExpression}");
            },
            cancellationToken);
    }
}
```

### 通过 Context 操作作业

```csharp
/// <summary>
/// {MethodSummary}
/// </summary>
/// <param name="{ParameterName}">{ParameterSummary}</param>
/// <returns>操作结果</returns>
public async Task<{ResultType}> {MethodName}({ParameterType} {ParameterName})
{
    if (AsgardContext.JobScheduler == null)
    {
        // 作业调度未启用，降级处理
        return {FallbackResult};
    }

    var result = await AsgardContext.JobScheduler.{Operation}(jobKey, cancellationToken);
    return result;
}
```
