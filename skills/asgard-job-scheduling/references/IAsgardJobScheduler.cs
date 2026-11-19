namespace Asgard.Abstractions.Job;

/// <summary>业务作用域的调度入口，提供作业和触发器管理及调度器状态查询。</summary>
public interface IAsgardJobScheduler : IAsgardFeature
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
    /// 调度作业（使用回调配置触发器）。
    /// </summary>
    /// <typeparam name="TJob">作业类型。</typeparam>
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
    /// <typeparam name="TJob">作业类型。</typeparam>
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
    /// <typeparam name="TJob">作业类型。</typeparam>
    /// <param name="jobKey">作业键。</param>
    /// <param name="replace">如果作业已存在是否替换。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddJobAsync<TJob>(JobKey jobKey, bool replace = false, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// 添加作业并关联触发器。
    /// </summary>
    /// <typeparam name="TJob">作业类型。</typeparam>
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
