# 自定义 Trace 请求采集

通过命名空间 `Asgard.AspNetCore.Core.Tracing` 下的 `AddAsgardTraceRequestCapture` 注册回调。默认 Yggdrasil 已接入追踪中间件，不需要逐个修改 Controller。

```csharp
builder.AfterServiceRegistration(services =>
{
    _ = services.AddAsgardTraceRequestCapture((capture, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!capture.IsError ||
            !int.TryParse(capture.HttpContext.Request.Query["page"].ToString(), out var page))
        {
            return ValueTask.FromResult<AsgardTraceRequestSnapshot?>(null);
        }

        return ValueTask.FromResult<AsgardTraceRequestSnapshot?>(new()
        {
            QueryString = QueryString.Create("page", page.ToString())
        });
    });
});
```

- 回调在请求结束、Trace 入队前执行；仅处理本来就会持久化的请求。使用 `HttpContext` 获取路由、请求或请求范围服务。
- 注册后，查询串、请求头和正文只从 `AsgardTraceRequestSnapshot` 取得。按接口选择可保存字段，正文宜返回业务摘要。返回 `null`、回调抛错或请求已取消时省略这些字段，仍保留基础记录。未注册时使用默认采集。
- 使用 `CanCaptureHeaders` 和 `CanCaptureBody` 判断本次是否允许头部和正文采集。它们仍受 Trace 配置限制；正常请求不保存头部和正文，超出 `MaxBodyBytes` 的摘要不保存。
- 读取正文前检查 `CanCaptureBody` 和流是否可定位；需要时移到开头，限制读取量并保持请求流打开。回调结束后框架恢复可定位流的原位置。`MaxBodyBytes` 限制输出摘要，不限制回调读取量。
- 回调对并发请求复用，支持异步并接收请求取消令牌。不要改变响应，也不要把原始请求数据放入业务异常、Trace 备注或参数日志；这些内容不经快照回调。
