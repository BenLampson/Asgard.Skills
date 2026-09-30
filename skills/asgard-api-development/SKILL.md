---
name: asgard-api-development
description: "编写或修改 Asgard Controller、/api 路由、VO、统一响应和分页接口。遵循 BaseController 与 Controller→Service→Repository 分层；授权表达式用 asgard-auth-authorization。"
---

# Asgard API Development

## 作用

用于开发遵循 Asgard 约定的 Web API 控制器。当你需要：
- 创建新的 API 控制器
- 修改现有的 API 接口
- 设计或调整后端路由前缀
- 添加分页列表接口
- 统一 API 响应格式
- 集成异常处理
- 遵循框架约定编写控制器代码

结构与规则边界：

- 控制器文件默认位于 `Controllers/`，目录权威见 `$asgard-plugin-structure`
- 输入模型默认位于 `Models/DTO`，输出模型默认位于 `Models/VO`
- 编码硬规则统一见 `$asgard-dotnet-10-csharp-14`

## 什么时候使用

- **需要创建控制器时** - 继承 `BaseController` 并遵循框架约定
- **需要统一响应格式时** - 使用 `Response<T>`、`PageResponse<T>`、`CursorResponse<T>`
- **需要列表分页时** - 使用标准页码分页或游标分页
- **需要异常处理时** - 启用全局异常处理中间件

## 核心约定

| 约定 | 说明 |
|------|------|
| **基类继承** | 必须继承 `BaseController`，不要直接继承 `ControllerBase` |
| **路由前缀** | 对外 HTTP API 必须统一挂在 `/api/...` 下；域名形态应为 `https://xxx.com/api/xxxx`，不要把业务接口暴露成 `https://xxx.com/xxxx` |
| **上下文注入** | 构造函数必须注入 `AbsAsgardContext` 并传给基类 |
| **响应统一** | 统一响应约束只作用于 Controller 对外返回；Controller 必须把最终 VO 包装成 `Response<T>`、`Response<object>`、`PageResponse<T>` 或 `CursorResponse<T>` 返回给前端 |
| **职责分离** | 控制器只做输入输出编排，业务逻辑放服务，数据访问放仓储 |
| **模型位置** | 输入 DTO 默认位于 `Models/DTO`，输出 VO 默认位于 `Models/VO` |
| **异常处理** | 启用 `UseAsgardExceptionHandler()` 全局处理，不要每个 Action 都写 try/catch |
| **身份读取** | 当前用户、租户、角色、权限统一从 `AsgardContext.IdentityContext` 读取，不要在 Controller 里手写 claim 解析 |
| **授权入口** | 需要按 `token_type`、角色、权限、scope、metadata 控制访问时，优先使用 `AsgardAuth*` 或 `AsgardAuthMatch(...)` |
| **前端长整型** | VO 中对外暴露的 `long` / `ulong` 标识、雪花 ID、计数字段如可能超过 JavaScript 安全整数范围，必须使用 Asgard 内置 JSON Converter 输出为字符串 |

## 框架授权 vs 业务租户边界

必须明确区分两层责任，避免“有 `[Authorize]` 就万事大吉”的误解：

- `AsgardAuth` / `[Authorize]` 负责声明式权限判断（你有没有访问某类能力的资格）
- 业务代码仍需自行校验资源归属边界（例如 path/query/body 中的 `tenantId` 是否与当前身份一致）

如果你需要区分 JWT 中的令牌类型，当前 `AsgardAuthMatch(...)` 已支持直接判断：

```csharp
[AsgardAuthMatch("token_type = 'BackendService'")]
```

不需要再把 `token_type` 手工复制到 `metadata.token_type` 才能参与授权。

换句话说，框架不会自动替你完成“请求参数租户 与 当前身份租户”的一致性校验。多租户接口必须显式做这一步。

## 强制要求

以下要求属于 Asgard Web API 的硬约束，不允许为了“方便”而放宽：

- 所有 Controller 必须继承 `BaseController`
- 所有对外后端 API 路由必须以 `/api` 作为第一段路径，例如 `[Route("api/[controller]")]`、`[Route("api/users")]`；除健康检查、静态文件、Swagger、OIDC discovery/JWKS 等框架或协议端点外，不要把业务接口挂在根路径
- 分层职责固定为：`Controller -> Service -> Repository -> Entity`
- 输出职责固定为：`Service` 产出 DTO，`Controller` 把 DTO 转成 VO 后再统一包装响应
- 所有 Controller Action 对外返回值必须统一使用 `Response<T>`、`Response<object>`、`PageResponse<T>` 或 `CursorResponse<T>`
- 普通查询、详情、创建、修改、删除等接口默认返回 `Response<T>` 或 `Response<object>`
- 页码分页列表接口必须返回 `PageResponse<T>`
- 游标分页 / 无限滚动列表接口必须返回 `CursorResponse<T>`
- 不允许 Controller 直接返回未包装的 VO、DTO、字符串、布尔值、数字、匿名对象或集合
- 不允许在 Controller 中再自定义另一套通用响应壳模型
- Swagger / OpenAPI 的 `ProducesResponseType` 也必须与统一响应模型保持一致
- 对于返回实体详情或单一资源的 Action，如果成功返回类型是 `Response<TVo>`，则 `404 NotFound` 的 `ProducesResponseType` 也应优先标注为 `Response<TVo>`，保持 Swagger 文档与统一响应壳的泛型语义一致

## 响应方法对照表

| 方法 | 使用场景 | 返回类型 |
|------|----------|----------|
| `Success<T>(data)` | 普通成功响应，带数据 | `Response<T>` |
| `Success(message)` | 成功响应，无数据 | `Response<object>` |
| `SuccessPage(data, totalCount, page, size)` | 标准页码分页 | `PageResponse<TItem>` |
| `SuccessCursor(data, hasMore, nextCursor, lastId)` | 游标分页（无限滚动） | `CursorResponse<TItem>` |
| `Fail<T>(code, message)` | 失败响应，自定义状态码 | `Response<T>` |
| `BadRequest<T>(message)` | 参数错误 | `Response<T>` |
| `NotFound<T>(message)` | 资源不存在 | `Response<T>` |
| `ServerError<T>(message)` | 服务器内部错误 | `Response<T>` |

## VO 前端兼容规则

前端 JavaScript `number` 不能安全表示超过 `2^53 - 1` 的整数。Asgard 已提供内置转换器，VO 中对外暴露的 `long` / `ulong` 字段仍保持 C# 强类型，但 JSON 输出应转为字符串，避免前端精度丢失。

### 单个字段推荐写法

```csharp
using Asgard.Abstractions.Serialization.Converters;
using System.Text.Json.Serialization;

namespace {Namespace}.Models.VO;

/// <summary>
/// 用户信息 VO。
/// </summary>
public class UserVo
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    [JsonConverter(typeof(LongToStringConverter))]
    public long Id { get; set; }

    /// <summary>
    /// 外部无符号 ID。
    /// </summary>
    [JsonConverter(typeof(ULongToStringConverter))]
    public ulong ExternalId { get; set; }
}
```

转换器读入时兼容 JSON 字符串和数字，写出时统一输出字符串。不要为了前端精度问题把后端 VO 属性类型改成 `string`；除非业务语义本来就是字符串。

`JsonSerializerOptionsFactory.ForFrontend` 也包含 `LongToStringConverter` 和 `ULongToStringConverter`，适合手动序列化或明确使用前端友好 options 的场景。Controller/MVC 响应不能假设已经全局使用该 options；生成 VO 时优先在字段上显式标注。

## 代码示例

需要编写该模块代码时，按场景读取 [实现示例](references/implementation-examples.md)，只采用与当前任务和目标版本匹配的示例。

## 推荐做法

- 每个控制器只负责一个业务领域
- 新增或改造业务接口时，先确认最终 URL 是 `https://xxx.com/api/xxxx` 形态
- 为每个 Action 添加 `[ProducesResponseType]` 注释，便于 Swagger 生成文档
- 详情类 / 单资源接口优先让 `200` 与 `404` 共享同一个 `Response<TVo>` 标注，减少 Swagger 类型语义漂移
- 通过 `AsgardContext` 获取当前用户、租户等上下文信息
- 需要区分用户登录令牌与后端服务令牌时，优先用 `token_type = 'UserLogin'` / `token_type = 'BackendService'`
- 增删改查涉及审计字段时，显式写入当前 `UserId`、必要时补充 `TenantId`
- 如果多个接口都依赖当前用户信息，优先在 Service 层统一封装获取逻辑
- 保持 Action 简洁，只做参数编排和结果返回
- 控制器文件放在 `Controllers/`，不要另起结构
- 需要文档时，在项目根目录 `app.yaml` 中开启 `host.swagger.enabled: true`
- 所有实现继续遵守 `$asgard-dotnet-10-csharp-14`
- 审查 Controller 时，优先检查返回类型是否仍然是 `Response` / `PageResponse` 家族

## 不要这样做

❌ 不要跳过分层边界，让 Controller 直接承担 Repository / Entity 访问

❌ 不要把业务 Controller 暴露在根路径，例如 `[Route("users")]` 或 `[Route("[controller]")]`；统一使用 `/api/...`

❌ 不要让 Service 直接返回给前端的响应壳模型，统一响应只属于 Controller 层

❌ 不要让 Controller 直接返回裸 `VO`、`DTO`、`string`、`bool`、`int`、`List<T>` 或 `IEnumerable<T>`

❌ 不要把业务逻辑直接写在 Action 里，保持职责分离

❌ 不要忽略分页响应的统一模型，所有列表接口都应该使用分页模型

❌ 不要在每个 Action 里重复编写大而全的 try/catch，交给全局异常处理

❌ 不要忘记注入 `AbsAsgardContext` 并传给基类构造函数

❌ 不要在 CRUD 代码里漏掉 `CreateBy`、`UpdateBy` 等审计字段，只因为“不知道当前用户从哪里拿”

❌ 不要在 Controller / Service 里到处直接手写 `HttpContext.User.FindFirst(...)`，统一走 `AsgardContext.IdentityContext`

❌ 不要为了前端 JavaScript 精度问题把本应为 `long` / `ulong` 的 VO 字段手工改成 `string` 或在 Mapper 里到处 `.ToString()`；应使用 `LongToStringConverter` / `ULongToStringConverter`

## 参考资料

完整源码拷贝请参考 `references/` 目录：
- `BaseController.cs` - 基础控制器实现
- `Response.cs` - 统一响应工厂
- `AsgardExceptionHandlerExtensions.cs` - 异常处理扩展

## 源码锚点

以下锚点用于核对“鉴权能力边界”与“框架职责边界”：

- `Common/Asgard.Abstractions.AspNetCore/Authorization/AsgardAuthAttributes.cs` - `AsgardAuth*` 特性与策略绑定
- `Common/Asgard.AspNetCore.Core/Authorization/AsgardAuthExpressionParser.Parser.cs` - `AsgardAuthMatch(...)` 支持的字段白名单
- `Common/Asgard.AspNetCore.Core/ServiceCollectionExtensions.cs` - `AddAsgardAspNetCore()` 注册授权能力
- `Host/Asgard.Yggdrasil.AspNetCore/YggdrasilHostBuilder.Configurator.cs` - 默认授权中间件接线
- `Common/Asgard.Abstractions.AspNetCore/Host/AuthOptions.cs` - `host.auth.enabled` 边界语义

结构规范请参考 `$asgard-plugin-structure`。
授权表达式细节请参考 `$asgard-auth-authorization`。

代码范本请参考 `templates/` 目录，可直接替换占位符使用。
