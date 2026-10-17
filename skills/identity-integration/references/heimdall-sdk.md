# Heimdall 官方 SDK 用法

以 SDK **5.6.0**、源码标签 `v5.6.0` 为接口基线。协议与能力清单见 [SDK API](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.6.0/sdk/API.md)。下面按使用环境选入口，不另写一套 Token 协议。

## 选择与安装

| 环境/需求 | 包与入口 | 接入方负责 |
|-----------|----------|------------|
| 浏览器 SPA | `@benlampson/heimdall-browser` → `HeimdallBrowserClient` | 回调页面、UI、业务数据与最终资源授权 |
| 无 Storage 的 TS 协议 | `@benlampson/heimdall-browser/protocol` → `HeimdallProtocolClient` | 登录/退出事务一次性消费、路由、凭据保存与调度 |
| C# 用户协议 | `Asgard.Heimdall.Client` → `HeimdallOAuthClient` | 同上；默认 public，机密客户端另配凭据提供器 |
| C# 服务 Token/目录 | 同包 → `AddHeimdallClient`、`HeimdallTokenProvider`、`HeimdallClientFactory` | 命名凭据提供器、业务权限决策 |
| Node 22+ | `@benlampson/heimdall-server` → `HeimdallServerClient`、`HeimdallDirectoryClient` | 服务端凭据、用户事务/存储、业务权限决策 |

浏览器与 Node 包是 ESM。先在目标注册表核对版本，将下面的 `{已发布SDK版本}` 替换为所选产品 Release 中实际已发布的版本；按目标运行时安装，浏览器 bundle 不导入 server 包：

```shell
npm install @benlampson/heimdall-browser@{已发布SDK版本}
# Node 项目使用：
npm install @benlampson/heimdall-server@{已发布SDK版本}
# .NET 项目使用：
dotnet add package Asgard.Heimdall.Client --version {已发布SDK版本}
```

源码合并与产品 tag 不等于三个包已发布，安装前分别核对 npm、NuGet 与 Release 记录；不把仅包含服务端的旧 tag 当 SDK 源码链接。若目标包尚未发布，可引用上述提交的 SDK 项目，或消费按该源码构建的本地 nupkg，并明确其来源；不自动触发生产发布。

三个 SDK 版本跟随 Heimdall 产品 Release。跨版本接入先核对方法签名和协议，不因示例自动升级已有依赖。完整构建/打包方法见 [SDK README](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.6.0/sdk/README.md)。

## 登记与可信配置

先取得已登记的 issuer、clientId、tenantId、applicationId、资源 audience 和 scopes。Authority、API 根地址与 Heimdall 管理前端地址分别配置；端点由 Discovery 获取，不从待验 Token 或当前 URL 猜发行者。

Public SPA 登记 Code + S256 PKCE、准确登录/退出回调及 Allowed CORS Origins；需要 RT 时授予 `refresh_token` 与 `offline_access`。`orders.api` 是示例 scope，需映射到 `orders-api` 受众，不能把 scope 和 audience 混成一个字段。服务端机密客户端使用已登记的 basic/post 和最小权限，不把 Secret 放进浏览器。

示例域名和 ID 应替换为实际登记值。HTTP 联调只有 loopback 可以显式允许；不要把 `allowLocalhostHttp` 当多账号功能开关。

## 浏览器：创建一个共享 Client

在浏览器应用的 auth/service 层创建一份实例，不在每个 React 渲染或页面里创建；SSR 服务端不实例化 BrowserClient：

```ts
import { HeimdallBrowserClient, hasAllPermissions } from '@benlampson/heimdall-browser';

export const heimdall = new HeimdallBrowserClient({
  authority: 'https://idp.example.com/tenant-a',
  clientId: 'orders-spa', tenantId: 'tenant-a', applicationId: 'orders-app',
  audience: 'orders-api', scope: 'openid profile email offline_access orders.api',
  redirectUri: 'https://orders.example.com/callback',
  postLogoutRedirectUri: 'https://orders.example.com/logout-complete',
  resourceBaseUrls: ['https://api.example.com/'],
  automaticRenew: true,
});

export async function startLogin() { await heimdall.login('/orders'); }
export async function chooseAccount() { await heimdall.switchAccount('/orders'); }
export async function completeLogin() {
  const result = await heimdall.handleLoginCallback();
  return result.returnPath; // 应用路由接纳安全回跳，不记录 tokens 或回调 URL。
}
export async function loadOrders() {
  const response = await heimdall.fetch('https://api.example.com/orders');
  if (!response.ok) throw new Error(`Orders HTTP ${response.status}`);
  return response.json(); // 业务层继续解析实际响应契约。
}
export function canCreateOrder() {
  return hasAllPermissions(heimdall.getSnapshot().identity, ['orders.create']);
}
```

七项字符串配置 `authority/clientId/tenantId/applicationId/audience/redirectUri/postLogoutRedirectUri` 都必需。`resourceBaseUrls` 可省略，但调用 `fetch` 前必须配置可信范围，否则返回 `untrusted_resource`。普通 `login` 不默认强制选账号；`switchAccount` 等同显式 `select_account`。服务端支持 `select_account login`，但 5.5.2 SDK 的 prompt 类型只接受 `none|login|consent|select_account` 单值，不传组合字符串。

应用在 `/callback` 调用 `handleLoginCallback`，接纳 `returnPath`；SDK 完成事务一次性消费、兑换、身份校验和候选提交。不要同时让原始 `oidc-client-ts` 或第二套 auth provider 消费同一回调。切换旧凭据的撤销是否确认可读 `previousRevocationConfirmed`，不能承诺其他 RP 会话同时退出。

应用在回调成功后替换路由；失败时也清理地址中的敏感回调参数，再呈现错误或原有效会话。SDK 不替应用清除 callback URL，不把参数留在 UI、日志或后续跳转地址中。

### 保存、刷新与 UI 生命周期

- 默认保存到 localStorage，要求 Web Locks；`sharedStorage:false` 不能绕过默认 localStorage 的锁要求。可选择 `storage:window.sessionStorage`、自定义 Storage，或 `storage:false` 仅实例内存；共享自定义实现标记 `sharedStorage:true` 以启用锁，但这不提供任意自定义 store 的外部变更通知。
- `storage:false` 只在实例内保留用户凭据，一次性 state/nonce/PKCE 事务仍临时存入 sessionStorage。整页跳转重建实例会丢失原凭据与版本，已登录后切换或统一退出回跳可能返回 `session_changed`；需跨跳转恢复会话时选默认 localStorage 或同标签 sessionStorage。需要完全不访问 window/Storage 时，使用 `/protocol`，由调用方安全保存并原子消费事务。
- `automaticRenew` 默认 **false**；示例显式开启。`getAccessToken()` 在需要时合并续期，`refreshAccessToken()` 主动换取并保存；`setAutomaticRenew(false)` 停止计时续期，但页面变为 visible 时仍会调用 `getAccessToken` 并可能按需续期。不要再添加另一套刷新计时器或存储锁。
- `getTokenSet()` 返回完整凭据，默认让 SDK 保存，业务仅按需使用。RT 轮换必须接纳新 RT，不长期缓存一次读出的 AT。
- `getSnapshot()` 是不含 AT/RT 的 UI 数据快照，不代表当前授权；通过 `subscribe(listener)` 观察变化，返回的取消订阅函数由组件清理。应用真正销毁 Client 时调用 `dispose()`；它停止计时器/监听，不执行退出或撤销。
- `getUserInfo()` 读取申请 scopes 允许的标准资料，校验 sub；`hasAllPermissions` 是导出的函数，控制 UI 可见性，资源 API 仍执行最终授权。
- `fetch` 取 Token 时可按需续期，只对允许的资源附加 Bearer，禁止重定向并处理凭据版本。收到资源 401 时结束仍匹配的捕获会话，**不自动刷新再重试该请求**；先读 status/ok，再处理正文，401 的正文读取可能因会话已结束而报 `session_changed`。不要叠加通用 401 重放。响应读取保护不覆盖直接消费 `Response.body` 流期间的业务状态，流式处理自行检查身份变化；不要把 `getAccessToken` 接到任意 HTTP 客户端后就宣称获得了同样的响应保护。

### 错误与恢复

| 浏览器结果 | 接入方处理 |
|------------|------------|
| `session_changed` | 丢弃旧结果并重读快照，不清较新的登录 |
| `not_authenticated` / `login_required` | 显示手动登录入口，不自动恢复 SSO |
| `renewal_failed` | 重读状态；SDK 已对 `invalid_grant` 精确清理，网络失败保留会话，不再笼统清空 |
| `previousRevocationConfirmed:false` | 新身份已提交，旧凭据撤销未确认，不能声称整个切换失败 |

`HeimdallBrowserError.code` 与 `HeimdallProtocolError.code/status` 用于分类，不能记录原始回调 URL、凭据或正文。后台自动刷新没有独立 `onRenewError` 回调；`subscribe` 提供身份/版本变化，业务操作仍需要显式错误处理。`dispose` 不保证取消在途任务。

### 两种退出

```ts
export async function leaveApplication() {
  const result = await heimdall.logoutApplication();
  return result.revocationConfirmed; // 本地已退出；false 表示远端撤销未确认。
}
export async function leaveSelectedSsoAccount() { await heimdall.logoutSso(); }
export async function completeSsoLogout() { await heimdall.handleLogoutCallback(); }
```

普通退出不调用 End Session，不清 OP Cookie；明确统一退出才调用 `logoutSso`，在登记的 `/logout-complete` 消费退出 state。退出后由 UI 停在可手动登录的页面，不能自动进入 SSO。远端撤销失败也不恢复本地登录。更完整边界见 [多账号会话契约](heimdall-multi-account.md)。

### 设备登录与纯协议

浏览器使用 `requestDeviceAuthorization()` 取得 device_code/user_code/verification_uri/时限；自行呈现验证地址和用户码，再调用 `waitForDeviceToken(device, signal)`，或单次 `exchangeDeviceCode(device, signal)`。SDK 遵守 interval、slow_down、截止与取消；浏览器便利层成功后保存结果。用户必须在 Heimdall 托管页面批准，SDK 不自动批准设备。

纯协议 `HeimdallProtocolClient.createLoginRequest({prompt,maxAge,responseMode})` 返回 URL 和包含 verifier 的上下文；接入方先原子消费已保存上下文，再调用 `exchangeCode(URLSearchParams, savedRequest, signal)`。不要记录上下文，也不要复用浏览器便利层的 `createLoginRequest(returnPath, options)` 签名。

## C#：服务 Token 与只读目录

下面的凭据提供器演示从进程环境获取 Secret；实际项目使用自己的 Secret Manager。`Version` 是非秘密轮换标识，改变后不复用旧服务 Token 缓存。命名选项没有 `ClientId` 属性，ClientId 来自 provider：

```csharp
using Asgard.Heimdall.Client;

public sealed class DirectoryCredentials : IHeimdallCredentialProvider
{
    public ValueTask<HeimdallClientCredentials> GetAsync(
        string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (name != "orders-directory") throw new InvalidOperationException("未知凭据名称。");
        return ValueTask.FromResult(new HeimdallClientCredentials(
            Required("HEIMDALL_CLIENT_ID"), Required("HEIMDALL_CLIENT_SECRET"),
            Required("HEIMDALL_SECRET_VERSION")));
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"缺少配置：{name}");
}
```

在宿主已有 `IServiceCollection` 注册，示例消费者展示调用链：

```csharp
using Asgard.Heimdall.Client;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<IHeimdallCredentialProvider, DirectoryCredentials>();
services.AddHeimdallClient("orders-directory", options =>
{
    options.Authority = "https://idp.example.com/tenant-a";
    options.ApiBaseUrl = "https://api.example.com";
    options.TenantId = "tenant-a"; options.ApplicationId = "orders-app";
});
using var provider = services.BuildServiceProvider();
var tokenProvider = provider.GetRequiredService<HeimdallTokenProvider>();
var serviceAt = await tokenProvider.GetAccessTokenAsync("orders-directory");
using var directory = provider.GetRequiredService<HeimdallClientFactory>().Create("orders-directory");
var permissions = await directory.GetUserPermissionsAsync("tenant-user-id");
var mayDispatch = permissions.HasAllPermissions(["orders.dispatch"]);
```

服务凭据使用登记的 `client_credentials` 与 basic/post，默认目录 scope=`heimdall.directory.read`、audience=`heimdall-directory-api`，固定 tenant/application。服务 Token 在内存缓存，可用 `CacheTokens=false` 关闭；它不使用用户 RT。目录返回 DTO，不写数据库、不默认缓存权限；HTTP/业务失败不能降级为空权限成功。真实业务继续检查 Active 与必需权限。

`AddHeimdallClient` 不注册 `HeimdallOAuthClient` 或 ASP.NET 认证。Asgard 资源 API 继续使用 `host.auth.jwt`；SDK 的离线验签不是实时撤销门禁。

## C#：用户协议入口

```csharp
using Asgard.Heimdall.Client;

using var oauth = new HeimdallOAuthClient(new HeimdallOAuthOptions
{
    Authority = "https://idp.example.com/tenant-a", ClientId = "orders-client",
    TenantId = "tenant-a", ApplicationId = "orders-app", Audience = "orders-api",
    Scope = "openid profile offline_access orders.api",
    RedirectUri = "https://orders.example.com/callback",
    PostLogoutRedirectUri = "https://orders.example.com/logout-complete"
});
var login = await oauth.CreateLoginRequestAsync(prompt: "select_account");
// 安全保存 login，向浏览器返回 login.Url；回调时原子消费事务。
// ExchangeCodeAsync(callbackParameters, savedRequest) 返回完整 HeimdallTokenSet。
```

默认 public/none；机密客户端设置已登记认证方法并向构造器传 `IHeimdallCredentialProvider`。用户 RT 使用 `RefreshTokenAsync(rt, subjectId, sessionId)` 绑定原身份，调用方保存轮换结果；设备流程使用 `RequestDeviceAuthorizationAsync`、`WaitForDeviceTokenAsync`。普通退出清自己的 Cookie/凭据并 `RevokeAsync`；统一退出用 `CreateLogoutRequestAsync(idToken)`，回调先一次性消费退出事务，再静态 `HeimdallOAuthClient.ValidateLogoutCallback(parameters,state)`。SDK 不创建路由、Cookie、用户存储或刷新作业。

## Node：服务与目录

```ts
import { HeimdallServerClient, HeimdallDirectoryClient,
  type HeimdallServerOptions } from '@benlampson/heimdall-server';

export function createDirectoryService(
  credentialProvider: NonNullable<HeimdallServerOptions['credentialProvider']>,
) {
  const server = new HeimdallServerClient({
    authority: 'https://idp.example.com/tenant-a', clientId: 'orders-directory',
    tenantId: 'tenant-a', applicationId: 'orders-app',
    audience: 'heimdall-directory-api', scope: 'heimdall.directory.read',
    authenticationMethod: 'client_secret_basic', credentialProvider,
    resourceBaseUrls: ['https://api.example.com/'],
  });
  const directory = new HeimdallDirectoryClient(server, 'https://api.example.com', 'tenant-a');
  return { server, directory };
}

export async function mayDispatch(service: ReturnType<typeof createDirectoryService>, userId: string) {
  const permissions = await service.directory.getUserPermissions(userId);
  return permissions.status === 'Active' && permissions.permissions.includes('orders.dispatch');
}
```

在服务启动时创建 Client，provider 异步返回 `{clientId,secret,version}`，clientId 必须与配置一致。`getClientCredentialsToken()` 返回完整结果；`cacheClientCredentials:false` 可关闭内存缓存。`server.fetch(url,init,userAt)` 的第三参数可传用户 AT；省略时取服务 AT，不把这两种身份混用。

Node 继承无存储协议方法 `createLoginRequest`、`exchangeCode`、`refreshToken`、`getUserInfo`、`revokeToken`、`createLogoutRequest`、`validateLogoutCallback`，另有同机密客户端的 `introspectToken`。没有命名 DI 或用户 Token 自动保存/调度。Node 目录权限结果只有字段，没有 C# 的 `HasAllPermissions` 方法。

## 校验、MCP 与未包含能力

- 默认 AT 格式为 jwt；opaque 必须与登记一致并显式配置，验签失败不自动降级。opaque AT 不提取角色权限；用户身份以验证的 ID Token 和标准 UserInfo 建立。
- `ValidateJwtAsync/validateJwt` 验签和身份边界；`DecodeJwt/decodeJwt` 明确未验证，不能用作授权。C# Token 字段为 PascalCase，TS 为 OAuth snake_case，`obtained_at` 是毫秒时间；不要混用模型。
- Introspection 只允许原 confidential Client 的 AT，不能代 public SPA 跨 client 查询。后通道接收、实时门禁和身份失效 Webhook 消费仍由接入方实现，见对应集成 skill。
- C# MCP 使用 `HeimdallMcpAuthentication.CreateMcpAuthenticationHeaders(url,ak,sk,"basic"|"headers")` 或 `CreateMcpBearerHeaders(url,at)`；Node 使用导出的 `createMcpAuthenticationHeaders(url,authentication)`。只对可信 HTTPS `/mcp` 地址使用，交给标准 MCP 客户端；Basic 为原始 AK:SK，SDK 不提供 HMAC 签名或 AK/SK 换 JWT，也不封装 tools/resources/tasks。
- 密码、MFA、Passkey、LDAP、SAML、社交登录由 Heimdall 托管页面完成；SDK 不提供 password grant、OP 账号枚举/审批接口、React/Vue UI/Hook、SCIM CRUD 或通知接收中间件。

用 SDK 不代表真实登记 Client 已验收。编译和模拟 HTTP 测试之外，按修改范围验证回调、切换、刷新、两种退出及真实目录结果；原生多标签页/BFCache 与生产联调分别报告，不能借其他 session 的服务端验收替代。

## 服务端创建普通租户用户（5.6.0）

现有 .NET / Node 服务端包提供独立的 `HeimdallUserProvisioningClient`：C# 从 `HeimdallClientFactory.CreateUserProvisioning(name)` 获取，Node 从 server 包导入并调用 `createUser`。显式配置 `heimdall.users.create` 和 `heimdall-users-api`，凭据来自服务端注入。Scope 必须由管理员授予 Client，角色来自当前应用 RBAC 的 `isUserDefault`，不得用具体业务对象或部署白名单决定权限。`isDefault` 仍用于新租户管理员，禁止复用为普通用户默认角色。

接口只接收用户名、邮箱、初始密码及可选展示名/手机号，拒绝归属与角色字段；归属来自已验证 Token。不返回用户 Token，不改 OIDC 登录，不向浏览器包添加创建接口。创建 POST 不自动重试。安装 5.6.0 的现有服务端包即可，无新包。开通、迁移、错误契约与完整示例见 [用户供给契约](../../heimdall-service-integration/references/user-provisioning.md) 和 [仓库接入指南](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.6.0/docs/backend-user-provisioning.md)。
