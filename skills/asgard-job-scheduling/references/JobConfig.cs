namespace Asgard.Core.Job;

/// <summary>
/// 作业调度器配置类，包含调度器配置和作业定义列表。
/// </summary>
/// <remarks>
/// 此类实现了 <see cref="ISystemConfig"/> 接口，与 Asgard 的配置体系集成。
/// 支持从合并的配置文件中加载，使用 "job" 作为路径前缀。
/// </remarks>
/// <example>
/// 配置示例（合并文件 config.yaml）：
/// <code>
/// job:
///   enabled: true
///   scheduler:
///     threadPoolSize: 10
///     maxBatchSize: 100
///     enableCluster: false
///     instanceId: "server-1"
///   jobs:
///     - name: "DataSyncJob"
///       group: "DataSync"
///       jobType: "MyApp.Jobs.DataSyncJob, MyApp"
///       description: "数据同步作业"
///       triggers:
///         - type: "cron"
///           cron: "0 0/5 * * * ?"
///           startNow: true
/// </code>
/// </example>
public class JobConfig : ISystemConfig
{
    /// <summary>
    /// 获取或设置是否启用作业调度模块。
    /// </summary>
    /// <remarks>
    /// 当设置为 false 时，整个作业调度模块将被禁用，不会注册任何调度服务。
    /// 默认值为 false。
    /// </remarks>
    /// <value>true 表示启用作业调度模块；false 表示禁用。</value>
    [ConfigPath("job.enabled", DefaultValue = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 获取或设置调度器配置。
    /// </summary>
    /// <remarks>
    /// 包含线程池大小、集群配置、实例标识等调度器相关配置。
    /// 对应 YAML 中的 job.scheduler 节点。
    /// </remarks>
    [ConfigPath("job.scheduler")]
    public JobSchedulerOptions Scheduler { get; set; } = new();

    /// <summary>
    /// 获取或设置作业定义列表。
    /// </summary>
    /// <remarks>
    /// 列表中的每个元素定义了一个作业及其触发器配置。
    /// 对应 YAML 中的 job.jobs 节点。
    /// </remarks>
    [ConfigPath("job.jobs")]
    public List<JobDefinitionOptions> Jobs { get; set; } = [];

    /// <summary>
    /// 验证配置的有效性。
    /// </summary>
    /// <remarks>
    /// 此方法在配置加载完成后自动调用。验证以下内容：
    /// <list type="number">
    ///   <item><description>Scheduler 配置对象不为 null</description></item>
    ///   <item><description>Jobs 列表不为 null</description></item>
    ///   <item><description>每个作业定义的名称和类型不能为空</description></item>
    ///   <item><description>每个作业至少要有一个触发器配置</description></item>
    /// </list>
    /// 注意：只有当启用了作业调度模块（Enabled = true）时，才会验证作业配置的有效性。
    /// 验证失败时将抛出 <see cref="InvalidOperationException"/> 异常。
    /// </remarks>
    /// <exception cref="InvalidOperationException">当配置无效时抛出。</exception>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Scheduler);

        if (Jobs is null)
        {
            throw new InvalidOperationException("Jobs list cannot be null.");
        }

        // 只有当启用了作业调度模块时，才验证作业配置
        if (Enabled && Jobs.Count > 0)
        {
            for (var i = 0; i < Jobs.Count; i++)
            {
                var job = Jobs[i];
                if (string.IsNullOrWhiteSpace(job.Name))
                {
                    throw new InvalidOperationException($"Job #{i} name is required.");
                }

                if (string.IsNullOrWhiteSpace(job.JobType))
                {
                    throw new InvalidOperationException($"Job '{job.Name}' type is required.");
                }

                if (job.Triggers is null || job.Triggers.Count == 0)
                {
                    throw new InvalidOperationException($"Job '{job.Name}' must define at least one trigger.");
                }

                foreach (var trigger in job.Triggers)
                {
                    ValidateTrigger(trigger, job.Name);
                }
            }
        }
    }

    /// <summary>
    /// 验证触发器配置的有效性。
    /// </summary>
    /// <param name="trigger">触发器配置。</param>
    /// <param name="jobName">作业名称（用于错误信息）。</param>
    /// <exception cref="InvalidOperationException">当触发器配置无效时抛出。</exception>
    private static void ValidateTrigger(TriggerOptions trigger, string jobName)
    {
        if (trigger is null)
        {
            throw new InvalidOperationException($"Job '{jobName}' trigger config cannot be null.");
        }

        var triggerType = trigger.Type?.ToLowerInvariant() ?? "simple";

        if (triggerType == "cron")
        {
            if (string.IsNullOrWhiteSpace(trigger.Cron))
            {
                throw new InvalidOperationException($"Job '{jobName}' cron trigger must provide a cron expression.");
            }
        }
        else if (triggerType == "simple")
        {
            if (string.IsNullOrWhiteSpace(trigger.Interval))
            {
                throw new InvalidOperationException($"Job '{jobName}' simple trigger must provide an interval.");
            }
        }
        else
        {
            throw new InvalidOperationException($"Job '{jobName}' trigger type '{trigger.Type}' is unsupported. Supported: cron, simple.");
        }
    }
}
