namespace Asgard.Abstractions.Job;

/// <summary>
/// 作业调度器核心接口，提供作业和触发器的管理功能。
/// </summary>
/// <remarks>
/// 这是 Asgard 框架中作业调度模块的核心接口，封装了 Quartz.NET 的核心功能。
/// 通过此接口，应用层可以方便地管理作业和触发器，而无需关心底层实现细节。
/// 主要功能包括：
/// <list type="bullet">
///   <item><description>作业的创建、删除、暂停、恢复和立即触发</description></item>
///   <item><description>触发器的创建、删除、暂停和恢复</description></item>
///   <item><description>调度器的启动和关闭</description></item>
///   <item><description>作业和触发器状态的查询</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // 获取调度器
/// var scheduler = serviceProvider.GetRequiredService<IJobScheduler>();
/// 
/// // 启动调度器
/// await scheduler.StartAsync();
/// 
/// // 注册作业
/// await scheduler.ScheduleJobAsync<MyJob>("my-job", trigger => 
/// {
///     trigger.WithCronSchedule("0 0/5 * * * ?");
/// });
/// 
/// // 暂停作业
/// await scheduler.PauseJobAsync(new JobKey("my-job"));
/// 
/// // 立即触发作业
/// await scheduler.TriggerJobAsync(new JobKey("my-job"));
/// 
/// // 关闭调度器
/// await scheduler.ShutdownAsync();
/// </code>
/// </example>
public interface IJobScheduler : IAsyncDisposable
{
    /// <summary>
    /// 获取调度器的名称。
    /// </summary>
    /// <value>调度器的实例名称。</value>
    string SchedulerName { get; }

    /// <summary>
    /// 获取一个值，指示调度器是否正在运行。
    /// </summary>
    /// <value>如果调度器正在运行返回 true，否则返回 false。</value>
    bool IsStarted { get; }

    /// <summary>
    /// 获取一个值，指示调度器是否已关闭。
    /// </summary>
    /// <value>如果调度器已关闭返回 true，否则返回 false。</value>
    bool IsShutdown { get; }

    /// <summary>
    /// 启动调度器。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 启动调度器后，触发器将开始触发作业执行。
    /// 如果调度器已经启动，此方法不会执行任何操作。
    /// </remarks>
    Task StartAsync();

    /// <summary>
    /// 关闭调度器。
    /// </summary>
    /// <param name="waitForJobsToComplete">是否等待正在执行的作业完成。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 关闭调度器后，将不再触发任何作业。
    /// 如果 waitForJobsToComplete 为 true，将等待正在执行的作业完成后再关闭。
    /// </remarks>
    Task ShutdownAsync(bool waitForJobsToComplete = true);

    /// <summary>
    /// 暂停所有作业。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 暂停调度器中的所有作业。
    /// 已经正在执行的作业将继续执行完成，但不会触发新的作业。
    /// </remarks>
    Task PauseAllAsync();

    /// <summary>
    /// 恢复所有作业。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 恢复之前暂停的所有作业。
    /// </remarks>
    Task ResumeAllAsync();

    /// <summary>
    /// 调度作业（使用回调配置触发器）。
    /// </summary>
    /// <typeparam name="TJob">作业类型。</type>
    /// <param name="jobKey">作业键。</param>
    /// <param name="configureTrigger">触发器配置回调。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 使用此方法可以方便地通过回调函数配置触发器。
    /// </remarks>
    Task ScheduleJobAsync<TJob>(JobKey jobKey, Action<TriggerOptions> configureTrigger, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// 调度作业（使用触发器选项）。
    /// </summary>
    /// <typeparam name="TJob">作业类型。</type>
    /// <param name="jobKey">作业键。</param>
    /// <param name="triggerOptions">触发器选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ScheduleJobAsync<TJob>(JobKey jobKey, TriggerOptions triggerOptions, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// 调度作业（使用作业定义和触发器列表）。
    /// </summary>
    /// <param name="jobDefinition">作业定义选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ScheduleJobAsync(JobDefinitionOptions jobDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加作业（不指定触发器）。
    /// </summary>
    /// <typeparam name="TJob">作业类型。</type>
    /// <param name="jobKey">作业键。</param>
    /// <param name="replace">如果作业已存在是否替换。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddJobAsync<TJob>(JobKey jobKey, bool replace = false, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// 添加作业并关联触发器。
    /// </summary>
    /// <typeparam name="TJob">作业类型。</type>
    /// <param name="jobKey">作业键。</param>
    /// <param name="triggerOptions">触发器选项。</param>
    /// <param name="replace">如果作业已存在是否替换。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddJobAsync<TJob>(JobKey jobKey, TriggerOptions triggerOptions, bool replace = false, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// 添加触发器。
    /// </summary>
    /// <param name="triggerKey">触发器键。</param>
    /// <param name="jobKey">作业键。</param>
    /// <param name="triggerOptions">触发器选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddTriggerAsync(TriggerKey triggerKey, JobKey jobKey, TriggerOptions triggerOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除作业。
    /// </summary>
    /// <param name="jobKey">作业键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果作业被删除返回 true，如果作业不存在返回 false。</returns>
    Task<bool> DeleteJobAsync(JobKey jobKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除触发器。
    /// </summary>
    /// <param name="triggerKey">触发器键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果触发器被删除返回 true，如果触发器不存在返回 false。</returns>
    Task<bool> DeleteTriggerAsync(TriggerKey triggerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 暂停作业。
    /// </summary>
    /// <param name="jobKey">作业键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task PauseJobAsync(JobKey jobKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复作业。
    /// </summary>
    /// <param name="jobKey">作业键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ResumeJobAsync(JobKey jobKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 暂停触发器。
    /// </summary>
    /// <param name="triggerKey">触发器键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task PauseTriggerAsync(TriggerKey triggerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复触发器。
    /// </summary>
    /// <param name="triggerKey">触发器键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ResumeTriggerAsync(TriggerKey triggerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 立即触发作业。
    /// </summary>
    /// <param name="jobKey">作业键。</param>
    /// <param name="jobData">作业数据（可选）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task TriggerJobAsync(JobKey jobKey, IJobData? jobData = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取作业状态。
    /// </summary>
    /// <param name="jobKey">作业键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>作业状态。</returns>
    Task<JobStatus> GetJobStatusAsync(JobKey jobKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查作业是否存在。
    /// </summary>
    /// <param name="jobKey">作业键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果作业存在返回 true，否则返回 false。</returns>
    Task<bool> CheckJobExistsAsync(JobKey jobKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查触发器是否存在。
    /// </summary>
    /// <param name="triggerKey">触发器键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>如果触发器存在返回 true，否则返回 false。</returns>
    Task<bool> CheckTriggerExistsAsync(TriggerKey triggerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取调度器的当前状态。
    /// </summary>
    /// <returns>调度器状态。</returns>
    Task<JobStatus> GetSchedulerStatusAsync();

    /// <summary>
    /// 注册作业监听器。
    /// </summary>
    /// <param name="listener">作业监听器。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddJobListenerAsync(IJobListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册触发器监听器。
    /// </summary>
    /// <param name="listener">触发器监听器。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddTriggerListenerAsync(ITriggerListener listener, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册调度器监听器。
    /// </summary>
    /// <param name="listener">调度器监听器。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddSchedulerListenerAsync(ISchedulerListener listener, CancellationToken cancellationToken = default);
}
