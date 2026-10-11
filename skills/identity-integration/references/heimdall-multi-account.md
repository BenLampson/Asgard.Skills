# Heimdall 多账号、刷新与退出

以 Heimdall **5.5.2** 实现为基线。接入其他版本先核对协议与客户端实现；架构计划、历史兼容文档中的待办不代表已发布行为。

## 默认能力与边界

- 多账号和后通道退出直接可用。`oidc.multi_account`、`oidc.backchannel_logout` 可省略；没有独立 `enabled` 开关，不为升级额外增加配置。
- 既有数据库升级必须执行目标版本所需迁移；新版本能力默认可用不等于旧表结构自动满足要求。多实例使用一致且持久的 Data Protection 密钥和应用标识。
- 容器 Cookie 仅绑定浏览器容器。容器可以包含多个有效 OP 会话和历史账号，不提供“最近账号就是当前用户”的身份保证；管理 API 仍需 Bearer 授权。
- RP 使用 `sub`、`tenant_id`、`sid` 关联身份；当前没有公开 `account_id` 参数或 claim。每次 RP 登录创建独立授权 ID，一个 OP `sid` 可关联多个 RP 授权，二者不能互换。
- 账号历史、被遗忘账号、`login_hint` 和授权 `id_token_hint` 不能代替认证。授权 hint 必须校验签名、issuer、client audience、租户与期限，只用于约束候选账号。
- 内部 `interaction`、`sessionId`、`console_entry` 属于绑定浏览器的一次性交互。第三方 RP 不自行构造控制台入口，也不用 query 参数里的主体选择登录身份。

升级范围服从用户要求：若已明确允许旧会话和 token 失效，可按授权范围清理认证状态，但必须保留用户、租户及业务数据。旧客户端兼容、备份与回退范围遵守任务和环境约定，不默认要求兼容回退包；用户明确拒绝新增目录或演练时，不重新增加。上线操作使用对应环境的发布流程。

## 授权与切换

| 请求 | 5.5.2 行为 |
|------|------------|
| `prompt=select_account` | 显式选择账号；不能默默采用最近账号 |
| `prompt=login` | 要求实际重新认证；确认按钮不产生新的认证时间 |
| `prompt=select_account login` | 先固定选择，再重新认证所选账号 |
| `prompt=none` | 筛选后没有有效账号返回 `login_required`；多个有效账号返回 `account_selection_required`，不展示交互页 |
| `max_age` | 非负整数秒，依据可信 `auth_time` 判断认证新鲜度；非法值返回 `invalid_request` |

RP 保持 Code + **S256 PKCE**、一次性 state、请求 nonce 和登记回调。验证 ID Token 后再接纳身份；获取 UserInfo 时核对其 `sub` 与 ID Token 一致。

切换应在独立候选存储中完成回调与身份校验，成功后在锁内检查原会话版本，再原子提交候选身份。`oidc-client-ts` 默认会写 userStore，仅调用 SDK 回调后才判断成功可能已覆盖原登录。取消、交互过期、`access_denied`、兑换或校验失败均保留原有效会话；迟到候选不能覆盖新登录。前端具体实现见 [会话并发与验证](../../asgard-admin-frontend/references/oidc-session-safety.md)。

## 刷新

- 只续原授权，固定 issuer/client/tenant/sub/sid，不读取最近 OP Cookie 来切换账号，不扩大 scope。
- 刷新不更新 `auth_time`，不延长 OP 绝对认证期限，也不让账号 B 的活动延长账号 A。
- 已结束授权返回 `invalid_grant`，扩大 scope 返回 `invalid_scope`。授权码兑换恢复原 nonce；刷新不接纳请求提供的新 nonce。
- 客户端区分网络失败与明确失效。旧刷新结果、旧 401 和旧清理任务都必须匹配捕获的会话版本与凭据后才提交或清理。

## 两种退出

| 动作 | 撤销边界 | RP 行为 |
|------|----------|---------|
| 普通“退出本应用” | 当前 client 的本次授权 | 停止续期、关闭本应用会话，按原 Discovery Revoke 撤销捕获的 RT/AT；不调用 End Session，不清 OP Cookie |
| 明确“统一退出此账号” | 指定 OP `sid` 及其浏览器来源授权 | 使用原 Discovery End Session、可信 ID Token、client、登记回跳与一次性退出 state；校验退出回调 state |

Public client 撤销使用 POST 表单和 `client_id`，不带 secret；Origin 登记在该 OIDC Client。捕获原会话和凭据后清本地状态；撤销网络失败仍完成本地退出，但报告远端撤销未确认，不把 token 持久化作重试队列。

统一退出缺少可信 hint/sid 时需明确目标确认，不能扩成同一主体的全部设备退出。可信 hint A 不能误结束当前容器的 B。退出 hint 可接受已过期但仍可验证的 ID Token；该例外不能用于登录授权。

有效 hint、明确 sid 且浏览器归属可验证时可以直接完成目标退出；缺失/不可信 hint、无法可靠定位目标或 Cookie 已过期等需要交互的分支才进入浏览器绑定、CSRF 和一次性确认，不要求每次统一退出都弹 OP 确认。RP 发起前取消保留本应用会话；若 RP 已关闭本地会话或撤销本应用授权，随后取消 OP 确认只保留尚未结束的 OP 会话，不能声称恢复已结束的 RP 授权。

浏览器 Code 来源和历史来源为 NULL 的授权在结束 sid 时撤销整个凭据族，**包括带 `offline_access` 的 RT**。只有服务端明确登记的非浏览器独立授权且具有 `offline_access` 才可能保留，例如实际支持的 `device_code`；当前 password grant 禁用。不要仅凭 scope、Cookie 是否存在或通知 URI 推断授权独立性。

## 后通道与资源 API 失效

- 第三方客户端可不登记后通道 URI，仍可正常登录/刷新；退出事件记为 `unsupported`。通知失败或不支持不会恢复已结束的 OP 会话或浏览器授权。
- Discovery 声明 back-channel/session 支持，当前不提供 front-channel 执行能力。仅内建 Console 地址由可信 issuer 推导，第三方不继承该地址。
- 接收端验证签名及允许算法、issuer、client audience、iat/exp、Logout Events、`sub`/`sid`、`jti` 防重放，并拒绝 nonce。按 issuer/client/sid 匹配，幂等接收，不因通知再发起 OP 退出；迟到旧 sid 不清新登录。
- 资源 API 若只离线验证 JWT，旧 AT 可能继续有效至到期。需要更快失效时明确采用短 AT、可用的在线内省或共享会话失效状态；后通道是通知机制，不是任意 API 的即时吊销器。
- Heimdall Introspection 仅供机密客户端查询同 client/tenant 的 token。Public SPA 不能持有 secret，也不能借别的 client secret 跨客户端内省。
- `/api/account/session-status` 属于内建 Console 会话契约；业务 SPA 使用自己的后端与实际提供的协议能力，不为调用此接口扩充 Heimdall 管理 API CORS。

服务端维护时不要给 `oidc_browser_session_boundary` 随意增加按 AT/RT TTL 清理的任务。5.5.2 的活动与结束边界行仍被保留，旧 Cookie/待完成请求可能引用结束边界；安全清理需要先定义并强制执行引用的绝对接受期限。

## 反向代理与 Cookie

5.5.2 `IdentityCookiePolicy` 使用配置中可信 `oidc.issuer`。公开 issuer 为 HTTPS 时，即使反向代理到后端的请求是 HTTP，也发出 Secure Cookie；实际 HTTPS 请求始终 Secure。缺失/非法 issuer 或非 loopback HTTP issuer 采用安全默认，不信请求 Host 或 `X-Forwarded-Proto` 来降低保护。

三个交互 Cookie 为 `__Host-Heimdall.Accounts`、`__Host-Heimdall.InteractionBinding`、`__Host-Heimdall.Logout`，使用 Secure、HttpOnly、SameSite=Lax、Path=/，无 Domain。身份票据保留 `Asgard.Identity` 名称并应用同一 Secure 策略。只有显式 HTTP loopback issuer 加 HTTP 请求允许开发 Cookie 名称与非 Secure；不要新增代理开关或把生产 Cookie 改为非 Secure。

## 接入验证

按改动覆盖实际风险：A→B 切换与失败保留 A；取消/过期及静默零/多账号；原身份刷新；普通退出不结束 OP；统一退出目标 sid，其他账号继续有效；浏览器 offline_access 随退出失效；旧回调/刷新/401 不破坏新登录；多标签页与页面恢复时清理已失效会话。

服务端测试使用独立测试数据库/schema，生产交互测试使用明确授权的测试租户和会话。配置静态检查、HTTP 200 或模拟事件不能代替浏览器回调、原生 BFCache、真实身份与数据结果的验证；报告具体已验证范围。

## 已核对来源

- [客户端接入说明（v5.5.2）](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.5.2/docs/oidc-multi-account-client.md)
- [服务端模型与部署说明（v5.5.2）](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.5.2/docs/oidc-multi-account-server.md)
- [客户端迁移清单（v5.5.2）](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.5.2/docs/oidc-client-migration.md)
- [Cookie 策略实现（v5.5.2）](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.5.2/be/Asgard.Heimdall/Identity/Infrastructure/IdentityCookiePolicy.cs)
