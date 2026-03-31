---
name: asgard-host-features
description: Asgard 宿主特性配置与用法 skill。Use when configuring or explaining host.staticFiles, host.cors, host.auth, host.swagger, host.rateLimiting, host.healthCheck, middleware order, tenant middleware placement, or host-managed web features in Asgard.
---
```
# Asgard 宿主 Web 功能配置

## 作用

本模块负责通过 `host.*` 配置统一管理 ASP.NET Core Web 宿主的各项功能。

什么时候使用本 skill：
- 配置新宿主的 `app.yaml` 时
- 需要开启/关闭某个 Web 功能
- 添加 CORS、认证、限流、健康检查等功能
- 调整中间件注册顺序
- 需要验证配置项有效性

## 配置结构

所有功能都在 `config/app.yaml` 的 `host.*` 节点下统一配置：

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
  issuer: "https://your-issuer.com/"
  audience: "your-audience"
  key: "your-secret-signing-key"
  tokenLifetimeMinutes: 1440
```

**多租户支持**：Asgard 从 JWT `issuer` 声明解析租户标识。

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
- 统一使用 `Response<T>` 响应格式
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
app.UseAsgardExceptionHandler();
app.UseHttpsRedirection();

// 静态文件
if (hostConfig.StaticFiles.Enabled)
{
    app.UseStaticFiles();
}

// CORS
if (hostConfig.Cors?.Enabled == true)
{
    app.UseCors();
}

// 限流
if (hostConfig.RateLimiting?.Enabled == true)
{
    app.UseRateLimiter();
}

// 认证和授权（授权必须在认证之后）
if (hostConfig.Auth?.Enabled == true)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// 租户中间件必须放在认证之后，业务之前
if (hostConfig.Tenant?.Enabled == true)
{
    app.UseAsgardTenant();
}

// 输出缓存
app.UseOutputCache();

// 端点映射
if (hostConfig.Swagger?.Enabled == true)
{
    app.MapOpenApi();
}
if (hostConfig.HealthCheck?.Enabled == true)
{
    app.MapHealthChecks(hostConfig.HealthCheck.Endpoint);
}
app.MapControllers();
```

**关键位置：**
- `UseAuthorization()` 必须放在 `UseAuthentication()` 之后
- `UseAsgardTenant()` 必须放在 `UseAuthentication()` 之后

## 代码模板

完整模板见 `templates/` 目录：
- `appyaml-host.yaml.template` - 完整宿主配置模板
- `MiddlewareOrder.cs.template` - 中间件顺序模板

## 参考源码

需要查看配置定义或验证规则时读 `references/`：
- `HostConfig.cs` - 宿主根配置类
- `StaticFileHostOptions.cs` - 静态文件配置选项

## 不要这样做

- ❌ 不要重复手动注册那些已经由 `host.*` 配置自动管理的能力
- ❌ 不要把认证、限流、健康检查这些宿主级功能写成每个插件各自一套
- ❌ 不要打乱中间件顺序，尤其不要把 `UseAuthorization()` 放在 `UseAuthentication()` 之前
- ❌ 不要把租户中间件放在认证之前
- ❌ Swagger 启用后，不要忘了给 API 添加 XML 注释说明
```
