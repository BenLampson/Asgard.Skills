namespace Asgard.Abstractions.Job;

/// <summary>宿主持有的调度器，负责调度进程的启动、全局暂停、关闭和资源释放。</summary>
public interface IJobScheduler : IAsgardJobScheduler, IAsyncDisposable
{
    /// <summary>
    /// 启动调度器。
    /// </summary>
    /// <param name="cancellationToken">宿主启动令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 启动调度器后，触发器将开始触发作业执行。
    /// 如果调度器已经启动，此方法不会执行任何操作。
    /// </remarks>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭调度器。
    /// </summary>
    /// <param name="waitForJobsToComplete">是否等待正在执行的作业完成。</param>
    /// <param name="cancellationToken">宿主停止令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>
    /// 关闭调度器后，将不再触发任何作业。
    /// 如果 waitForJobsToComplete 为 true，将等待正在执行的作业完成后再关闭。
    /// </remarks>
    Task ShutdownAsync(bool waitForJobsToComplete = true, CancellationToken cancellationToken = default);

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

    /// <summary>在最终服务容器就绪后准备调度器，供插件注册作业。无准备步骤的实现可沿用默认行为。</summary>
    Task PrepareAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
