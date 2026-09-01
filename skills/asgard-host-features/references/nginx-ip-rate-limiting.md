# Nginx 后的 Asgard IP 限流

## 适用场景

当 `host.rateLimiting.ip.enabled: true` 且 Asgard 位于 Nginx、负载均衡器或其他反向代理之后时，必须同时配置代理端转发和 ASP.NET Core 端可信代理处理。

仅添加 `X-Forwarded-For` 请求头不够。Asgard IP 层读取 `HttpContext.Connection.RemoteIpAddress`；只有 Forwarded Headers 中间件在限流前消费可信的 `X-Forwarded-For` 后，该值才会变成真实客户端地址。

## 单层 Nginx 推荐配置

拓扑：

```text
Client → Nginx → Asgard/Kestrel
```

```nginx
upstream asgard_backend {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 443 ssl;
    server_name api.example.com;

    # TLS 证书配置按部署环境提供。

    location / {
        proxy_http_version 1.1;
        proxy_pass http://asgard_backend;

        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;

        # 单层边缘 Nginx 直接覆盖客户端传入的同名头，防止伪造。
        proxy_set_header X-Forwarded-For $remote_addr;
    }
}
```

如果 Nginx 前面还有受信任的负载均衡器，应先用 Nginx Real IP 模块只信任该负载均衡器，再生成转发头。不要在没有可信代理边界时直接信任任意客户端传入的 `X-Forwarded-For`。

`$proxy_add_x_forwarded_for` 会保留已有 `X-Forwarded-For` 并追加当前连接地址，适合已明确定义可信代理链的部署；单层边缘 Nginx 优先使用 `$remote_addr` 覆盖输入，配置更容易审计。

## Asgard/Kestrel 端必须提前消费转发头

当前 Yggdrasil 没有 `host.forwardedHeaders` 配置，普通 `ConfigureMiddleware(...)` 扩展点位于实例/IP 限流之后，因此不能在那里调用 `UseForwardedHeaders()`。

需要在 starter 中注册一个 `IStartupFilter`，让 Forwarded Headers 中间件包在 Yggdrasil 宿主管道最外层。

`Program.cs`：

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

await YggdrasilHost.CreateBuilder("app.yaml")
    .BeforeServiceRegistration(services =>
    {
        _ = services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = 1;

            // 填写 Kestrel 实际看到的 Nginx 地址，不是公网客户端地址。
            options.KnownProxies.Add(IPAddress.Parse("10.0.0.10"));
        });

        _ = services.AddTransient<IStartupFilter, TrustedForwardedHeadersStartupFilter>();
    })
    .Build()
    .RunAsync();
```

`TrustedForwardedHeadersStartupFilter.cs`：

```csharp
namespace YourProject;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

/// <summary>
/// 在 Yggdrasil 宿主管道之前应用可信转发头。
/// </summary>
internal sealed class TrustedForwardedHeadersStartupFilter : IStartupFilter
{
    /// <summary>
    /// 将转发头中间件插入宿主管道最前端。
    /// </summary>
    /// <param name="next">后续宿主管道。</param>
    /// <returns>组合后的应用配置委托。</returns>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            _ = app.UseForwardedHeaders();
            next(app);
        };
    }
}
```

关键要求：

- `KnownProxies` 填 Nginx 对 Kestrel 发起连接时使用的固定地址。多代理或动态容器网络应改为严格限定的可信网络，并让 `ForwardLimit` 与真实代理跳数一致。
- 不要清空可信代理限制，也不要把 `ForwardLimit` 设置为无限，除非部署拓扑和安全边界明确要求且已审计。
- 不建议用 `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` 作为生产捷径；该开关会以更宽松的方式启用转发头处理，无法表达这里要求的精确信任边界。
- Kestrel 应只监听内网或回环地址，并由防火墙/容器网络阻止公网绕过 Nginx 直接访问。
- `X-Real-IP` 不是 ASP.NET Core Forwarded Headers 的默认输入；IP 限流依赖的是正确消费后的 `RemoteIpAddress`。

## 验证

部署后至少验证以下行为：

1. 记录或临时返回 `HttpContext.Connection.RemoteIpAddress`，确认它是客户端地址而不是 Nginx 地址。
2. 同一客户端连续请求超过 IP 配额后收到 `429`。
3. 从另一个客户端地址发起请求仍有独立额度。
4. 客户端手工伪造 `X-Forwarded-For` 不能切换自己的限流桶。
5. 直接访问 Kestrel 的公网路径不可达。

官方依据：

- [Microsoft：Configure ASP.NET Core to work with proxy servers and load balancers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [Microsoft：Kestrel security considerations](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/security-considerations?view=aspnetcore-10.0)
- [Nginx：ngx_http_proxy_module](https://nginx.org/en/docs/http/ngx_http_proxy_module.html)
