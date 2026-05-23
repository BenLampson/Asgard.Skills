---
name: asgard-tracing-observability
description: Asgard 轻量链路追踪与可观测性 skill。Use when working with Asgard request tracing, independent Trace persistence, asgard_trace records, error/5xx request snapshots for AI replay, AsyncLocal context flow, Trace notes/tags, framework step logging, middleware-based observation, or reproducing unit-test inputs from runtime logs.
---

# Asgard Tracing And Observability

## 作用

用于处理 Asgard 的轻量级链路追踪能力。这个能力的目标不是做全量审计，也不是抓 CLR 全函数调用栈，而是：

- 在 HTTP 请求范围内建立统一的 `TraceId`
- 自动记录 Asgard 框架可控入口的关键步骤
- 保留足以帮助定位问题、反推测试输入、解释分支走向的信息
- 在请求结束时输出一条汇总日志
- 允许业务代码通过 `AsgardContext.Trace` 追加备注、标签与分支说明
- 可按 `Trace.*` 配置把 Trace 独立持久化到 `asgard_trace`，错误请求可采集脱敏后的 request snapshot，供 AI 复现问题

## 什么时候使用

- **需要排查“框架到底走了哪条链路”时**
- **需要从运行日志反推出单元测试入参时**
- **需要在 Controller / Service / Repository 中追加业务备注时**
- **需要开启或解释 `Trace.Enabled` / `Trace.CaptureAllRequest` 独立持久化时**
- **需要为异常或 5xx 请求保存可复现现场，供 AI 调试时**
- **需要判断该用中间件追踪、Context 备注还是仓储自动步骤时**
- **需要解释 `AsyncLocal<T>` 在 Asgard 中如何承载请求追踪时**

## 核心结论

| 主题 | 结论 |
|------|------|
| **追踪边界** | 默认是 HTTP 请求 + 框架可控公开入口，不承诺覆盖私有方法、自调用、第三方库内部方法 |
| **上下文承载** | 通过 `AsyncLocal` 保存当前请求的追踪会话 |
| **入口建立** | 顶层中间件进入请求时创建追踪会话，结束时统一输出 |
| **框架步骤** | 控制器入口、仓储入口等框架可控位置自动记录步骤 |
| **用户扩展** | 业务代码通过 `AbsAsgardContext.Trace` 只能追加备注/标签/分支说明，不能篡改框架步骤 |
| **参数策略** | 简单类型全量，复杂对象只记录类型名、关键标识字段和集合数量摘要 |
| **普通日志策略** | 每请求输出一条轻量 `AsgardTrace` 摘要日志，不再把完整 request snapshot 拼进普通日志 |
| **持久化策略** | `Trace.Enabled=false` 不落库；`Trace.Enabled=true` 只持久化异常/5xx；`Trace.CaptureAllRequest=true` 时所有请求落库 |
| **错误快照** | 异常/5xx 请求默认保存脱敏 headers/body/identity，正常请求即使落库也不保存 body |

## 推荐入口

| 场景 | 正确入口 |
|------|----------|
| **业务代码追加说明** | `AsgardContext.Trace?.AddNote(...)` |
| **补充结构化标签** | `AsgardContext.Trace?.AddTag(...)` |
| **补充分支判断** | `AsgardContext.Trace?.AddBranch(...)` |
| **自动记录请求链路** | 顶层请求追踪中间件 |
| **自动记录控制器执行** | MVC 全局 Action Filter |
| **自动记录仓储关键调用** | `AbsAsgardRepositoryBase` 统一封装 |
| **Trace 独立落库** | `Trace.Enabled` + 独立 `FreeSqlTraceStore` |
| **AI 复现错误现场** | `asgard_trace` 表里的 request snapshot + steps/tags/notes/branches |

## Trace 独立持久化配置

当前 Trace 持久化是独立能力，不再依赖普通日志表保存完整请求现场。

```yaml
Trace:
  Enabled: true
  CaptureAllRequest: false
  Provider: MySQL
  ConnectionString: "Server=127.0.0.1;Database=my_app;"
  TableName: asgard_trace
  BatchSize: 100
  Period: 2
  RetentionDays: 7
  CleanupIntervalMinutes: 60
  MaxBodyBytes: 65536
  CaptureHeaders: true
  CaptureBody: true
  CaptureIdentity: true
```

行为边界：

- `Trace.Enabled: false`：不写 `asgard_trace`，错误请求也不写。
- `Trace.Enabled: true` 且 `Trace.CaptureAllRequest: false`：只持久化异常或 HTTP 5xx。
- `Trace.Enabled: true` 且 `Trace.CaptureAllRequest: true`：所有请求持久化。
- 错误/5xx 请求保存 request snapshot；正常请求即使落库，也只保存轻量链路，不保存 body。
- 请求头和 JSON body 中的 `Authorization`、`Cookie`、`password`、`token`、`secret`、`key` 等敏感字段必须掩码。

实现边界：

- Trace 持久化使用独立 FreeSql 连接，不复用业务 `IFreeSql`。
- 启动时自动同步 `asgard_trace` 表结构。
- 请求线程只入队，不同步写数据库。
- 后台 worker 按 `BatchSize` / `Period` 批量写入，并按 `RetentionDays` + `CleanupIntervalMinutes` 清理旧 Trace。
- 普通日志中的 `AsgardTrace` 只保留摘要，并通过 `TraceId` 与 `asgard_trace` 关联。

## `AsgardContext.Trace` 的定位

`Trace` 是给业务侧补充说明用的，不是给业务方接管框架追踪结构用的。

可以做：

- 追加一条备注
- 追加一组标签
- 记录“当前为什么走到这个分支”
- 标记“这一步对应哪个测试意图”

不要做：

- 不要把完整大对象序列化塞进去
- 不要把每个细碎私有方法都手工记一遍
- 不要把它当审计日志系统
- 不要依赖它保证覆盖任意函数

## 代码示例

### 在业务服务里追加备注

```csharp
/// <summary>
/// 创建订单
/// </summary>
/// <param name="command">订单命令</param>
/// <returns>订单标识</returns>
public async Task<Guid> CreateOrderAsync(CreateOrderCommand command)
{
    AsgardContext.Trace?.AddTag("OrderId", command.OrderId.ToString());
    AsgardContext.Trace?.AddBranch("OrderCreate", "ValidateBeforePersist");
    AsgardContext.Trace?.AddNote("创建订单前已完成业务规则校验，可据此反推测试前置条件。");

    return await _orderRepository.InsertAsync(command.ToEntity());
}
```

### 在控制器里补充测试线索

```csharp
/// <summary>
/// 根据订单标识获取详情
/// </summary>
/// <param name="id">订单标识</param>
/// <returns>订单详情</returns>
[HttpGet("{id:guid}")]
public async Task<ActionResult<Response<OrderDto>>> GetByIdAsync(Guid id)
{
    AsgardContext.Trace?.AddTag("Endpoint", "Orders/GetById");
    AsgardContext.Trace?.AddNote("该接口默认期望有效 Guid 且命中已登录用户上下文。");

    var result = await _orderService.GetByIdAsync(id);
    return Success(result);
}
```

## 参数记录规则

- 简单类型：`string`、数值、`bool`、`Guid`、`DateTime`、`DateTimeOffset`、`TimeSpan`、枚举等，直接记录
- 字符串过长时，只保留前缀并附带长度
- 明显敏感字段（如 password、token、secret、authorization）默认掩码
- 集合类型只记录类型名和数量
- 复杂对象只记录类型名，以及 `Id`、`TenantId`、`UserId`、`Code`、`Name`、`Status`、`Type` 这类关键字段摘要

## 推荐做法

- 先把这套能力当“定位问题与反推测试的轻量追踪”，不要上来就做审计系统
- 业务代码只追加最有信息量的备注与标签
- 优先记录“为什么走到这个分支”，而不是记录“这行代码执行了”
- 对用户投诉“框架行为很神奇”的场景，优先看请求汇总日志里的步骤链和备注
- 对 AI 修 bug 的场景，优先查 `asgard_trace`，拿 `TraceId` 关联普通日志和错误请求快照
- 正常请求全量落库必须显式使用 `Trace.CaptureAllRequest=true`，避免数据库膨胀
- 如果问题本身属于 `AsgardContext` 使用方式，再联动 `$asgard-context-usage`

## 不要这样做

❌ 不要把 `Trace` 当作任意对象转储入口，复杂对象不要深序列化

❌ 不要要求它覆盖所有私有方法或 CLR 层面任意函数

❌ 不要在单个请求里打印多条重复步骤日志，默认只保留汇总日志

❌ 不要在业务代码里改写框架自动步骤，用户侧只追加备注和标签

❌ 不要把完整 request snapshot 塞回普通日志表；完整现场应进入 `asgard_trace`

❌ 不要让正常请求默认保存 body；body 只属于错误快照或明确的调试场景

## 源码锚点

- `Common/Asgard.Abstractions/AbsAsgardContext.cs` - `Trace` 入口抽象
- `Common/Asgard.Core/AsgardContext.cs` - `Trace` 实现接入点
- `Common/Asgard.Abstractions/Tracing/TraceOptions.cs` - Trace 独立持久化配置
- `Common/Asgard.Core/Tracing/TraceRecord.cs` - `asgard_trace` 持久化实体
- `Common/Asgard.Core/Tracing/FreeSqlTraceStore.cs` - Trace 后台队列与批量写入入口
- `Common/Asgard.AspNetCore.Core/Tracing/AsgardRequestTraceMiddleware.cs` - 请求追踪中间件
- `Common/Asgard.AspNetCore.Core/Tracing/AsgardTraceQueueItemFactory.cs` - Trace 持久化队列项构造与快照脱敏
- `Common/Asgard.AspNetCore.Core/Tracing/AsgardTraceActionFilter.cs` - MVC 入口追踪
- `Common/Asgard.Abstractions/Data/AbsAsgardRepositoryBase.Trace.cs` - 仓储入口追踪封装
