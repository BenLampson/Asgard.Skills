# 作业示例

## 作业实现

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
    /// <returns>表示异步操作的任务</returns>
    public async Task ExecuteAsync(IJobExecutionContext context)
    {
        {ExecuteBody}
    }
}
```

## 插件初始化

```csharp
/// <summary>在插件初始化作用域中登记作业。</summary>
protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
{
    var scheduler = GetAsgardContext().JobScheduler;
    if (scheduler.Status == AsgardFeatureStatus.Disabled) return;
    await scheduler.ScheduleJobAsync<{JobName}>(new JobKey("{JobKey}"),
        new TriggerOptions { Type = "cron", Cron = "{CronExpression}" }, cancellationToken);
}
```

## 检查作业

```csharp
/// <summary>检查一个已装配的调度器中是否存在指定作业。</summary>
public Task<bool> JobExistsAsync(JobKey jobKey, CancellationToken cancellationToken) =>
    AsgardContext.JobScheduler.CheckJobExistsAsync(jobKey, cancellationToken);
```
