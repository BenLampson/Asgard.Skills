---
name: asgard-host-features
description: Asgard 宿主特性配置与用法 skill。Use when configuring or explaining host.staticFiles, host.cors, host.auth, host.swagger, host.rateLimiting, host.healthCheck, middleware order, tenant middleware placement, or host-managed web features in Asgard. For host.auth, this skill covers host-side JWT Bearer registration, middleware wiring, and config semantics, not frontend login flows or PKCE design.
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
  rateLimiting: ... # 全局限流
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
    allowAnyMethod: true
    allowAnyHeader: true
```

如果需要指定特定来源：

```yaml
cors:
  enabled: true
  defaultPolicy:
    origins:
      - "https://yourdomain.com"
      - "http://localhost:3000"
    allowAnyMethod: true
    allowAnyHeader: true
```

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

### 全局限流（host.rateLimiting）

```yaml
rateLimiting:
  enabled: true
  permitLimit: 100    # 一个窗口允许多少请求
  windowSeconds: 60    # 窗口大小（秒）
```

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
app.UseAsgardStaticFiles();
app.UseRouting();

if (hostConfig.Cors?.Enabled == true)
{
    app.UseCors();
}

if (hostConfig.RateLimiting?.Enabled == true)
{
    app.UseRateLimiter();
}

if (hostConfig.Auth?.Enabled == true)
{
    app.UseAuthentication();
}

app.UseAsgardTenant();

// 插件或外部中间件扩展点
configureMiddleware?.Invoke(app);

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
- `UseAuthorization()` 需要在插件或外部中间件之后统一执行，避免扩展链路还没补充身份信息就提前鉴权
- 不要再用旧版“认证和授权一起包进同一个 if，然后再执行租户中间件”的写法

## 代码模板

完整模板见 `templates/` 目录：
- `appyaml-host.yaml.template` - 完整宿主配置模板
- `MiddlewareOrder.cs.template` - 中间件顺序模板

## 参考源码

需要查看配置定义或验证规则时读 `references/`：
- `HostConfig.cs` - 宿主根配置类
- `StaticFileHostOptions.cs` - 静态文件配置选项

## 源码锚点

以下锚点用于减少二次解读偏差（按“事实 -> 代码”快速回查）：

- `Common/Asgard.AspNetCore.Core/ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 注册 `AsgardAuth` policy
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Services.cs` - `host.auth.enabled` 与默认 JWT 注册逻辑
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Configurator.cs` - 中间件顺序与统一 `UseAuthorization()`
- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - `AsgardAuth*` 特性统一使用 `AsgardAuth` policy
- `Common/Asgard.Abstractions.AspNetCore/Host/AuthOptions.cs` - `host.auth.enabled` 配置语义

## 不要这样做

- ❌ 不要重复手动注册那些已经由 `host.*` 配置自动管理的能力
- ❌ 不要把认证、限流、健康检查这些宿主级功能写成每个插件各自一套
- ❌ 不要打乱中间件顺序，尤其不要把 `UseAuthorization()` 放在 `UseAuthentication()` 之前
- ❌ 不要把租户中间件放在认证之前
- ❌ 不要假设关闭 `host.auth.enabled` 就等于整个授权链路失效，它只表示宿主不再代你注册默认 JWT Bearer
- ❌ 不要忘记 `tenant_id` 可能由宿主在 token 校验后自动补充，不要自己再写一份互相冲突的补 claim 逻辑
- ❌ Swagger 启用后，不要忘了给 API 添加 XML 注释说明
