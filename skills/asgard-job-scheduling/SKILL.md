---
name: asgard-job-scheduling
description: "配置或实现 Asgard 作业调度，包括 JobConfig、cron/simple 触发器、运行时注册、plugin.yaml 自动装配和 Context 调用。"
---

# Asgard Job Scheduling

## 作用

用于配置和使用 Asgard 作业调度模块。基于 Quartz.NET 提供 cron 定时任务和简单间隔任务的调度能力，支持静态配置和动态注册两种方式。

## 什么时候使用

- **需要配置全局作业调度** - 在项目根目录 `app.yaml` 中配置 `job.enabled` 和作业列表
- **插件需要自带作业** - 在 `plugin.yaml` 中配置插件自带作业，启动时自动注册
- **需要动态注册作业** - 在插件初始化阶段通过 `IJobScheduler` 动态注册
- **需要运行时操作作业** - 通过 `AbsAsgardContext.JobScheduler` 操作作业（暂停、恢复、删除、立即触发）
- **需要实现自定义作业** - 实现 `IJob` 接口编写作业逻辑

## 配置约定

### 全局配置结构

```yaml
job:
  enabled: {Enabled}
  scheduler:
    threadPoolSize: {ThreadPoolSize}
    maxBatchSize: {MaxBatchSize}
    enableCluster: {EnableCluster}
    instanceId: "{InstanceId}"
  jobs:
    - name: "{JobName}"
      group: "{JobGroup}"
      jobType: "{JobFullTypeName}, {AssemblyName}"
      description: "{JobDescription}"
      triggers:
        - type: cron
          cron: "{CronExpression}"
          startNow: {StartNow}
```

### 触发器类型对照

| 类型 | 必需字段 | 说明 |
|------|----------|------|
| `cron` | `cron` | Cron 表达式，支持标准 Quartz 语法 |
| `simple` | `interval` | 固定间隔，格式如 `00:05:00` 表示每 5 分钟 |

### 常用 Cron 表达式

| 表达式 | 说明 |
|--------|------|
| `0 0 * * * ?` | 每小时整点执行 |
| `0 0/5 * * * ?` | 每 5 分钟执行 |
| `0 0 0 * * ?` | 每天午夜执行 |
| `0 0 1 * * ?` | 每天凌晨 1 点执行 |
| `0 0 * * * MON-FRI` | 工作日每小时执行 |

## 访问方式

通过 `AbsAsgardContext.JobScheduler` 获取调度器，该属性可为 `null`（模块未启用时），必须做空检查后使用。

## 常用操作

| 方法 | 用途 |
|------|------|
| `ScheduleJobAsync<TJob>(jobKey, configureTrigger)` | 调度作业（回调配置触发器） |
| `AddJobAsync<TJob>(jobKey)` | 添加作业（不指定触发器） |
| `DeleteJobAsync(jobKey)` | 删除作业 |
| `PauseJobAsync(jobKey)` | 暂停作业 |
| `ResumeJobAsync(jobKey)` | 恢复作业 |
| `TriggerJobAsync(jobKey)` | 立即触发一次 |
| `GetJobStatusAsync(jobKey)` | 获取作业状态 |
| `CheckJobExistsAsync(jobKey)` | 检查作业是否存在 |

## 代码示例

需要编写该模块代码时，按场景读取 [实现示例](references/implementation-examples.md)，只采用与当前任务和目标版本匹配的示例。

## 推荐做法

- 启用模块必须设置 `job.enabled: true`，默认是 false
- `JobKey` 保持稳定命名，不要随意变化
- 静态作业定义在配置，动态注册在插件 `InitializeAsync` 阶段做
- 通过 `AbsAsgardContext.JobScheduler` 访问，始终做空检查
- 插件自带作业放在 `plugin.yaml` 中，由框架自动加载

## 不要这样做

❌ 不要在 `ConfigureServices` 阶段注册动态作业，应该在初始化阶段做

❌ 不要忽略 `AbsAsgardContext.JobScheduler` 可能为 null，模块可以被禁用

❌ 不要同一个作业使用随机变化的 JobKey，会导致重复注册

❌ 不要给触发器留空，每个作业至少要有一个触发器

❌ 不要在 Cron 表达式中写错格式，Quartz 对语法要求严格

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `JobConfig.cs` - 作业调度配置类
- `JobManager.cs` - 作业调度管理器
- `IJobScheduler.cs` - 作业调度器接口

代码范本请参考 `templates/` 目录：
- `app.yaml.template` - 配置片段范本，合并到项目根目录 `app.yaml`
- `JobImplementation.cs.template` - 作业实现范本
- `DynamicRegistration.cs.template` - 动态注册范本
- `JobOperationViaContext.cs.template` - 运行时操作范本
