---
name: asgard-host-features
description: Asgard 宿主特性配置与用法 skill。Use when configuring or explaining host.staticFiles, host.cors, host.auth, host.swagger, host.tsGen, host.rateLimiting, host.healthCheck, middleware order, tenant middleware placement, or host-managed web features in Asgard. For host.auth, this skill covers host-side JWT Bearer registration, middleware wiring, and config semantics, not frontend login flows or PKCE design.
---

# Asgard 宿主 Web 功能配置

## 作用

本模块负责通过 `host.*` 配置统一管理 ASP.NET Core Web 宿主的各项功能。

什么时候使用本 skill：
- 配置新宿主的 `app.yaml` 时
- 需要开启/关闭某个 Web 功能
- 添加 CORS、宿主默认 JWT 认证接线、限流、健康检查等功能
- 调整中间件注册顺序
- 需要验证配置项有效性

如果问题重点是“前端怎么登录”“Web 为什么要走 PKCE”“IDP、前端、Asgard API 之间怎么协作”，优先切到 `$identity-integration`；本 skill 主要负责宿主 `host.*` 配置和默认 Bearer JWT 接线。

## 配置结构

所有功能都在项目根目录 `app.yaml` 的 `host.*` 节点下统一配置：

```yaml
host:
  application:
    name: {application_name}
    version: {application_version}
    environment: {environment}
  kestrel:
    endpoints:
      http:
        url: http://*:{port}
  staticFiles: ...    # 静态文件
  cors: ...          # CORS 跨域
  auth: ...         # JWT 认证
  swagger: ...      # Swagger 文档
  tsGen: ...        # 可选 TypeScript 客户端导出
  rateLimiting: ... # 实例、IP、用户分层限流
  healthCheck: ...  # 健康检查
```

## 功能说明

### 静态文件（host.staticFiles）

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `enabled` | 是否启用 | `true` |
| `webRootPath` | 静态资源根目录 | `wwwroot` |
| `requestPath` | URL 访问前缀 | `""`（根路径）|
| `enableDefaultFiles` | 是否启用默认文件（index.html）| `true` |

**约束：**
- `requestPath` 要么为空字符串，要么必须以 `/` 开头

### CORS 跨域（host.cors）

```yaml
cors:
  enabled: true
  defaultPolicy:
    allowAnyOrigin: true
    allowedOrigins: []   # allowAnyOrigin 为 true 时可省略
    allowAnyMethod: true
    allowAnyHeader: true
    allowCredentials: false
    preflightMaxAgeSeconds: 600
```

完整配置（先关闭任意来源，再逐一列来源）：

```yaml
cors:
  enabled: true
  defaultPolicy:
    allowAnyOrigin: false
    allowedOrigins:
      - "https://yourdomain.com"
      - "http://localhost:3000"
    allowAnyMethod: false
    allowAnyHeader: false
    allowCredentials: true
    preflightMaxAgeSeconds: 1800

  # 可选：按名称拆分策略（可供插件/中间件按需使用）
  policies:
    admin-api:
      allowAnyOrigin: false
      allowedOrigins:
        - "https://admin.yourdomain.com"
      allowAnyMethod: true
      allowAnyHeader: true
      allowCredentials: true
      preflightMaxAgeSeconds: 1800
```

> 可直接抄这个区分：  
> 1. 单点调试：`allowAnyOrigin: true`（最快速）。  
> 2. 生产环境：`allowAnyOrigin: false + allowedOrigins: [...] + allowCredentials: true`（或 false）。  

### CORS 常见误区（务必避坑）

- `allowedOrigins` 字段名不能写成 `origins`；否则不会生效。
- `allowAnyOrigin: true` 与 `allowCredentials: true` 不能同时打开，启动会抛配置错误。
- `allowAnyOrigin: false` 时必须至少配置一个 `allowedOrigins`（不能为空数组）。
- `UseCors()` 位置固定在 `UseRouting()` 之后，`UseAuthorization()` 之前，由 `host` 中间件顺序模板统一承接。
- `host.cors.defaultPolicy.allowedOrigins` 是宿主资源 API 的调用方策略，不是 OIDC Client 注册表；不要把每个业务 SPA 的 Origin 机械追加到 IDP 宿主 YAML。
- OIDC Token、UserInfo、Revoke 等浏览器协议端点应优先使用 OIDC Client 自身登记的 Allowed CORS Origins，并由 IDP 的协议策略提供器处理。
- 如果业务 SPA 只是读取姓名、邮箱、头像等标准资料，应使用 Discovery 的 `userinfo_endpoint`；不要为了调用 Heimdall `/api/account/me` 而扩充宿主 CORS。

### JWT 认证（host.auth）

```yaml
auth:
  enabled: true
  jwt:
    issuerTemplate: "https://idp.example.com/realms/{tenantId}"
    audience: "your-audience"
    requireHttpsMetadata: true
    discoveryCacheMinutes: 60
    jwksCacheMinutes: 60
```

**关键行为：**

- 宿主内置认证当前是 Bearer JWT 模式
- 认证实现基于 OIDC discovery + JWKS 自发现
- `host.auth.enabled` 只控制宿主默认 JWT 注册与 `UseAuthentication()` 接线
- 即使关闭宿主内置 JWT，`UseAuthorization()`、`AsgardAuth`、租户和身份上下文链路仍可能由外部认证方案继续工作
- 宿主会在 `OnTokenValidated` 时检查当前身份是否已有 `tenant_id`
- 如果 token 中没有 `tenant_id`，但 `issuer` 能匹配 `issuerTemplate`，宿主会自动补上 `tenant_id` claim

### 框架托底事实

为了避免把“模板示例”误读成“必须手写全套安全注册”，这里明确框架默认托底行为：

- `AddAsgardAspNetCore()` 会自动注册 `AsgardAuth` policy（`AsgardAuthConstants.PolicyName`）
- Yggdrasil 默认管道会统一执行 `UseAuthorization()`
- 因此，使用 `AsgardAuth*` 特性时，通常不需要再额外手写 `AddAuthorization()` 才能生效

### `host.auth.enabled` 语义对照

| 配置值 | 语义 | 影响范围 |
|--------|------|----------|
| `true` | 宿主注册默认 JWT Bearer，并接入 `UseAuthentication()` | 启用宿主内置认证主体构建 |
| `false` | 仅关闭宿主默认 JWT 与对应 `UseAuthentication()` 接线 | 不关闭授权系统、不移除 `AsgardAuth` policy、不停用 `UseAuthorization()` |

补充说明：

- `false` 不是“禁用所有认证授权能力”，而是“宿主不再代管默认 JWT”
- 认证主体可以改由插件、网关、反向代理、外部中间件等方案提供
- 只要请求上最终存在可用身份主体，`AsgardAuth` 仍会参与授权计算

### 认证、身份上下文与租户的关系

需要把这几个概念分清：

- `UseAuthentication()` 负责把来访请求解析成 `ClaimsPrincipal`
- `UseAsgardTenant()` 负责根据当前身份建立租户上下文
- `IAsgardIdentityContext` / `AbsAsgardContext.IdentityContext` 负责向业务代码暴露当前身份快照
- `UseAuthorization()` 和 `AsgardAuth` 负责基于这些信息做鉴权

也就是说，业务代码真正应该读取的是 `AsgardContext.IdentityContext?.UserInfo`，而不是自己重新拼装 claim。

### Swagger/OpenAPI（host.swagger）

```yaml
swagger:
  enabled: true
  title: "你的 API 名称"
  version: "v1"
  description: "API 描述"
```

**推荐做法：**
- 为所有控制器和操作添加 XML 注释
- Controller 对外返回统一使用 Asgard 的 `Response` 家族响应模型
- 非分页接口使用 `Response<T>` / `Response<object>`，分页接口使用 `PageResponse<T>` / `CursorResponse<T>`
- Swagger 会自动包含注释信息

### TypeScript 客户端导出（host.tsGen）

```yaml
tsGen:
  enabled: true
```

关键行为：

- TsGen 是可选开发工具，默认关闭，不是所有前端项目的强制依赖
- 只有 `host.tsGen.enabled: true` 且运行环境为 `Development` 时才映射 `/asgard-tsgen`
- 选择 TsGen 的项目才需要为目标控制器添加 `[AsgardTsGen]`
- 每次生成会完整重建 `common/`、`controller/`、`models/` 纯生成目录
- 不使用 TsGen 的项目可以选择 OpenAPI、共享手写客户端或其他契约方案

### 分层限流（host.rateLimiting）

`host.rateLimiting` 是宿主总开关。请求按以下顺序经过各层：

```text
实例总量 → IP → Authentication/Tenant/外部身份 → 用户 → Authorization
```

根节点现有扁平字段继续表示“当前后端实例共享的总量桶”，不是 IP 桶。`ip` 和 `user` 是可选增强层：未配置或 `enabled: false` 时不启用该层。

```yaml
host:
  rateLimiting:
    enabled: true

    # 兼容旧配置：当前实例内所有请求共享
    policy: FixedWindow
    permitLimit: 600
    windowSeconds: 60
    queueLimit: 0

    # 可选：按连接 IP 独立分桶
    ip:
      enabled: true
      policy: FixedWindow
      permitLimit: 100
      windowSeconds: 60
      queueLimit: 0

    # 可选：在身份建立后按稳定主体独立分桶
    user:
      enabled: true
      policy: FixedWindow
      permitLimit: 60
      windowSeconds: 60
      queueLimit: 0
```

每一层都支持 `FixedWindow`、`SlidingWindow`、`TokenBucket`，并使用同一组算法参数：

| 配置项 | 说明 |
|--------|------|
| `policy` | `FixedWindow`、`SlidingWindow` 或 `TokenBucket` |
| `permitLimit` | 固定窗口/滑动窗口许可数 |
| `windowSeconds` | 窗口秒数 |
| `segmentsPerWindow` | 滑动窗口分段数 |
| `tokenLimit` | 令牌桶容量 |
| `tokensPerSecond` | 每秒补充令牌数 |
| `queueLimit` | 等待队列长度，默认 `0` |

关键语义：

- 旧 YAML 无需迁移；没有 `ip`、`user` 时仍只运行原有实例总量桶。
- 多实例部署时，每个实例各自维护实例、IP、用户桶；它不是 Redis 分布式限流。
- IP 层使用 `HttpContext.Connection.RemoteIpAddress`，并规范化 IPv4-mapped IPv6 地址；反向代理场景必须保证可信转发头在限流前已经生效，否则多个客户端可能落入代理 IP 的同一个桶。
- 用户层只处理已认证且有稳定身份声明的请求，身份键按 `tenant_id + user_id`、`sub`、`client_id`、`application_id` 的优先级选择。匿名请求或缺少稳定身份标识的请求跳过用户层，但仍受实例与 IP 层保护。
- 任意一层超限都返回 HTTP `429`。端点标注 `[DisableRateLimiting]` 时跳过宿主限流；用户层不会重新解析业务端点上的 `[EnableRateLimiting("named-policy")]`，因此不会覆盖或破坏业务命名策略。
- 宿主只调用一次官方 `UseRateLimiter()`：它承载认证前的实例与 IP 组合限流。认证后的用户层由 Yggdrasil 专用中间件承载。不要为了三层限流手工连续调用多个 `UseRateLimiter()`。

使用 Nginx 或其他反向代理并开启 IP 层时，必须读取 [Nginx 后的 Asgard IP 限流](references/nginx-ip-rate-limiting.md)。该参考同时给出 Nginx 转发头、ASP.NET Core 可信代理、Yggdrasil 提前接线和防伪造验证方式；不要只复制 Nginx 的一半配置。

### 健康检查（host.healthCheck）

```yaml
healthCheck:
  enabled: true
  endpoint: "/health"
```

## 推荐中间件顺序

**必须严格遵守此顺序，否则认证、授权、租户功能会出问题：**

```csharp
// 推荐的中间件注册顺序
// Nginx 场景由 IStartupFilter 在此管道之前插入 UseForwardedHeaders()。
app.UseAsgardStaticFiles();
app.UseRouting();

if (hostConfig.Cors?.Enabled == true)
{
    app.UseCors();
}

if (hostConfig.RateLimiting?.Enabled == true)
{
    // Yggdrasil 内部在同一个 GlobalLimiter 中串联实例桶与可选 IP 桶。
    app.UseRateLimiter();
}

if (hostConfig.Auth?.Enabled == true)
{
    app.UseAuthentication();
}

app.UseAsgardTenant();

// 插件或外部中间件扩展点
configureMiddleware?.Invoke(app);

// host.rateLimiting.user.enabled=true 时，Yggdrasil 在此处自动执行用户层。
// 宿主应用不要自行再注册第二个 UseRateLimiter()。

// 插件中间件之后统一进入授权
app.UseAuthorization();

if (hostConfig.Swagger?.Enabled == true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (hostConfig.HealthCheck?.Enabled == true)
{
    app.MapHealthChecks(hostConfig.HealthCheck.Endpoint);
}

app.MapControllers();
```

**关键位置：**
- `UseRouting()` 必须在认证链路之前，确保终结点信息可被认证授权系统识别
- `UseAuthentication()` 在启用宿主内置 JWT 时接入
- `UseAsgardTenant()` 必须位于认证之后，这样才能基于身份建立租户上下文
- 实例与 IP 层位于认证前；用户层位于认证、租户和外部身份扩展之后、授权之前
- `UseAuthorization()` 需要在插件或外部中间件之后统一执行，避免扩展链路还没补充身份信息就提前鉴权
- 三层宿主限流由框架自动接线，不要通过重复调用 `UseRateLimiter()` 模拟分层
- 不要再用旧版“认证和授权一起包进同一个 if，然后再执行租户中间件”的写法

**版本迁移提醒：**
- 修复 `PluginWebAppDefaults.RunAsync<TPlugin>()` 快速入口后，starter 不需要再为了 `[Authorize]` 手工补 `UseAuthorization()`。
- 已经手工补过 `UseAuthentication().UseAuthorization()` 的旧 starter 通常仍可运行，但后续维护时应逐步删掉重复授权中间件，避免授权策略重复执行。
- 如果 `host.auth.enabled: false` 且认证主体由插件或外部方案提供，只补提供身份所需的 `UseAuthentication()` 或自定义认证中间件，`UseAuthorization()` 仍交给宿主默认链路。

## 代码模板

完整模板见 `templates/` 目录：
- `appyaml-host.yaml.template` - 完整宿主配置模板
- `MiddlewareOrder.cs.template` - 中间件顺序模板
- `nginx-asgard-ip-rate-limit.conf.template` - 单层边缘 Nginx 的安全转发头模板

## 参考源码

需要查看配置定义或验证规则时读 `references/`：
- `HostConfig.cs` - 宿主根配置类
- `StaticFileHostOptions.cs` - 静态文件配置选项
- `TsGenHostOptions.cs` - 可选 TypeScript 客户端导出配置
- `nginx-ip-rate-limiting.md` - Nginx 转发头、可信代理接线与 IP 限流验证

## 源码锚点

以下锚点用于减少二次解读偏差（按“事实 -> 代码”快速回查）：

- `Common/Asgard.AspNetCore.Core/ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 注册 `AsgardAuth` policy
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Services.cs` - `host.auth.enabled` 与默认 JWT 注册逻辑
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Configurator.cs` - 中间件顺序与统一 `UseAuthorization()`
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilRateLimiterFactory.cs` - 实例/IP 组合限流与用户身份分区键
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilUserRateLimitingMiddleware.cs` - 认证后的用户层限流
- `Common/Asgard.Abstractions.AspNetCore/Host/RateLimitingOptions.cs` - 旧版实例字段及 `ip`、`user` 配置入口
- `Common/Asgard.Abstractions.AspNetCore/Host/RateLimitingPartitionOptions.cs` - IP/用户层算法参数
- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - `AsgardAuth*` 特性统一使用 `AsgardAuth` policy
- `Common/Asgard.Abstractions.AspNetCore/Host/AuthOptions.cs` - `host.auth.enabled` 配置语义

## 不要这样做

- ❌ 不要重复手动注册那些已经由 `host.*` 配置自动管理的能力
- ❌ 不要把认证、限流、健康检查这些宿主级功能写成每个插件各自一套
- ❌ 不要把根级 `host.rateLimiting` 扁平字段解释成 IP 限流；它始终是单实例共享总量桶
- ❌ 不要为实例、IP、用户三层分别重复调用官方 `UseRateLimiter()`，框架已经按正确阶段接线
- ❌ 不要只在 Nginx 写 `X-Forwarded-For` 就认为 IP 限流已经生效；Kestrel 必须在限流前消费可信转发头
- ❌ 不要在生产环境无条件信任所有 `X-Forwarded-For` 来源，否则客户端可以伪造 IP 绕过分桶
- ❌ 不要打乱中间件顺序，尤其不要把 `UseAuthorization()` 放在 `UseAuthentication()` 之前
- ❌ 不要把租户中间件放在认证之前
- ❌ 不要假设关闭 `host.auth.enabled` 就等于整个授权链路失效，它只表示宿主不再代你注册默认 JWT Bearer
- ❌ 不要忘记 `tenant_id` 可能由宿主在 token 校验后自动补充，不要自己再写一份互相冲突的补 claim 逻辑
- ❌ Swagger 启用后，不要忘了给 API 添加 XML 注释说明
