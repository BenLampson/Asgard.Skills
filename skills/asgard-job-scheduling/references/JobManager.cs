namespace Asgard.Core.Job;

/// <summary>
/// 作业调度管理器实现，负责作业调度系统的初始化、连接验证和生命周期管理。
/// </summary>
/// <remarks>
/// <para>
/// 此类独立于 IoC 容器，在系统启动阶段（Phase 3）完成初始化。
/// 初始化时会创建并启动 Quartz 调度器，注册配置中定义的作业。
/// </para>
/// </remarks>
public sealed class JobManager : IJobManager
{
    private readonly JobConfig _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<JobManager> _logger;
    private QuartzJobScheduler? _scheduler;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="JobManager"/> 类的新实例。
    /// </summary>
    /// <param name="config">作业配置。</param>
    /// <param name="loggerFactory">日志工厂。</param>
    /// <param name="logger">日志器。</param>
    public JobManager(JobConfig config, ILoggerFactory loggerFactory, ILogger<JobManager> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _config = config;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IJobScheduler Scheduler => _scheduler ?? throw new InvalidOperationException("作业调度管理器尚未初始化，请先调用 InitializeAsync。");

    /// <inheritdoc/>
    public bool IsStarted => _scheduler?.IsStarted ?? false;

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("正在初始化作业调度系统...");

        _config.Validate();

        var schedulerLogger = _loggerFactory.CreateLogger<QuartzJobScheduler>();
        _scheduler = new QuartzJobScheduler(_config, _loggerFactory, schedulerLogger);

        try
        {
            await _scheduler.StartAsync();
            _logger.LogDebug("作业调度系统初始化完成（调度器: {SchedulerName}）", _scheduler.SchedulerName);
        }
        catch (Exception ex)
        {
            _scheduler = null;
            throw new InvalidOperationException($"作业调度器启动失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task ShutdownAsync(bool waitForJobsToComplete = true)
    {
        if (_scheduler is not null && _scheduler.IsStarted && !_scheduler.IsShutdown)
        {
            _logger.LogInformation("正在关闭作业调度器...");
            await _scheduler.ShutdownAsync(waitForJobsToComplete);
            _logger.LogInformation("作业调度器已关闭");
        }
    }

    /// <inheritdoc/>
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (_scheduler is null)
        {
            throw new InvalidOperationException("作业调度管理器尚未初始化，无法设置 ServiceProvider。");
        }

        _scheduler.SetServiceProvider(serviceProvider);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_scheduler is not null)
        {
            await _scheduler.DisposeAsync();
        }
    }
}
