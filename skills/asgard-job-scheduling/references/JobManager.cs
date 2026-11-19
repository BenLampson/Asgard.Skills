namespace Asgard.Core.Job;

/// <summary>
/// 作业调度管理器实现，负责作业调度系统的初始化、连接验证和生命周期管理。
/// </summary>
/// <remarks>
/// <para>
/// 宿主通过最终服务容器传入选中的调度器，独立创建管理器时由管理器创建并释放调度器。
/// 独立使用 InitializeAsync 时会创建并启动调度器；宿主使用 PrepareAsync 先注册再按生命周期启动。
/// </para>
/// </remarks>
public sealed class JobManager : IJobManager
{
    private readonly JobConfig _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<JobManager> _logger;
    private IJobScheduler? _scheduler;
    private bool _ownsScheduler = true;
    private IServiceProvider? _serviceProvider;

    /// <summary>宿主按最终 DI 选择调度器，容器或调用者提供的后端保留其原有释放所有权。</summary>
    internal void UseScheduler(IJobScheduler scheduler, bool ownsScheduler)
    {
        _scheduler = scheduler;
        _ownsScheduler = ownsScheduler;
    }
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
    public Task InitializeAsync(CancellationToken cancellationToken = default) => InitializeCoreAsync(true, cancellationToken);

    /// <summary>准备内存调度器和配置作业但不执行作业，供宿主在最终容器及插件就绪后再启动。</summary>
    /// <param name="cancellationToken">准备阶段取消令牌。</param>
    /// <returns>准备完成的任务。</returns>
    /// <remarks>独立调用方仍可使用 InitializeAsync 一次完成准备和启动。</remarks>
    public Task PrepareAsync(CancellationToken cancellationToken = default) => InitializeCoreAsync(false, cancellationToken);

    /// <summary>共用准备、启动失败和资源清理路径，保证宿主和独立调用的所有权一致。</summary>
    private async Task InitializeCoreAsync(bool startScheduler, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("正在初始化作业调度系统...");
        _config.Validate();
        // 已准备的调度器不可覆盖；后续启动也进入相同的失败清理路径。
        if (_scheduler is null)
        {
            var schedulerLogger = _loggerFactory.CreateLogger<QuartzJobScheduler>();
            _scheduler = new QuartzJobScheduler(_config, _loggerFactory, schedulerLogger, _serviceProvider);
        }

        try
        {
            if (startScheduler) await _scheduler.StartAsync(cancellationToken);
            else if (_serviceProvider is not null) await _scheduler.PrepareAsync(_serviceProvider, cancellationToken);
            else if (_scheduler is QuartzJobScheduler quartz) await quartz.InitializeAsync(cancellationToken);
            _logger.LogDebug("作业调度系统初始化完成（调度器: {SchedulerName}）", _scheduler.SchedulerName);
        }
        catch (Exception ex)
        {
            try
            {
                if (_ownsScheduler) await _scheduler.DisposeAsync();
                else await _scheduler.ShutdownAsync(waitForJobsToComplete: false, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                throw new InvalidOperationException(
                    $"作业调度器启动失败且释放资源失败: {ex.Message}",
                    new AggregateException(ex, cleanupException));
            }
            finally
            {
                _scheduler = null;
            }

            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new InvalidOperationException($"作业调度器启动失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task ShutdownAsync(bool waitForJobsToComplete = true, CancellationToken cancellationToken = default)
    {
        if (_scheduler is not null && !_scheduler.IsShutdown)
        {
            _logger.LogInformation("正在关闭作业调度器...");
            await _scheduler.ShutdownAsync(waitForJobsToComplete, cancellationToken);
            _logger.LogInformation("作业调度器已关闭");
        }
    }

    /// <inheritdoc/>
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ObjectDisposedException.ThrowIf(_disposed, this);
        _serviceProvider = serviceProvider;
        if (_scheduler is QuartzJobScheduler quartz) quartz.SetServiceProvider(serviceProvider);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_ownsScheduler && _scheduler is not null) await _scheduler.DisposeAsync();
    }
}
