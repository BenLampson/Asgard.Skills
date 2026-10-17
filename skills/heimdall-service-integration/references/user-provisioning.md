# 服务端普通用户供给（Heimdall 5.6.0）

适用于业务系统希望在自己的页面添加本租户普通用户，并由业务后端调用 Heimdall。用户随后仍通过现有 OIDC 授权码/PKCE 登录。服务 Token 不兑换或代发用户 Token。

## 配置与边界

1. 在已有 Client 管理中登记绑定单一租户、应用的 confidential Client，允许 `client_credentials`，显式授予 `heimdall.users.create`。Scope 已由产品注册，映射受众 `heimdall-users-api`。
2. 在应用 Manifest 的 Tenant 角色中设置 `isUserDefault=true`，发布并同步租户应用；或通过已有租户应用自定义角色 API 设置。`isDefault` 是原有新租户管理员默认标记，两者互斥。没有有效普通角色时拒绝创建，不回退管理员。
3. Client、租户、应用及绑定必须启用，Manifest 同步成功。服务实时复核 Client grant/scopes、归属和角色权限状态。租户、应用和 Client 只从已验证 Token 决定。
4. 权限依据为注册且授予 Client 的 Scope、可信 Token 归属与当前应用 RBAC；不得硬编码具体租户、应用、Client、角色或权限组合，也不得添加部署白名单。公开 Scope/受众常量属于 API 契约，不是业务对象白名单。
5. Client Secret 由后端部署环境或 Secret Manager 注入，不提交 Git、不进入镜像、不下发浏览器。业务后端负责鉴别发起创建操作的业务用户。

## 请求与响应

从 Discovery 的 `token_endpoint` 使用已登记 basic/post 认证，发送 `grant_type=client_credentials&scope=heimdall.users.create`。随后携带 Bearer 调用 `POST /api/backend/users`：

```json
{
  "username": "alice",
  "email": "alice@example.test",
  "password": "<12–128 字符的初始密码>",
  "displayName": "Alice"
}
```

`username/email/password` 必填，`displayName/phone` 可选。拒绝任何未知字段，包括租户、应用、角色、状态、ID 和审计字段。Query 也不能覆盖归属。用户、密码登录标识、当前应用普通角色和授权版本在同一事务提交，复用现有哈希、重复检查及审计。

成功 HTTP 200，统一 `code/message/data`，data 包含 `id/tenantId/applicationId/username/displayName/email/phone/roleCodes`，不含密码或用户 Token。未认证 401、授权/归属/策略无效 403、输入无效 400、重复账号或并发冲突 409。未知服务错误按标准错误处理，不回传凭据。SDK 不自动重试创建 POST；网络结果不确定时先查询确认，不能盲目重复创建。

## 现有 SDK

```shell
dotnet add package Asgard.Heimdall.Client --version 5.6.0
npm install @benlampson/heimdall-server@5.6.0
```

- C#：命名 `HeimdallClientOptions` 配置实际 Authority/API 根地址、预期 TenantId/ApplicationId、`Scope="heimdall.users.create"`、`Audience="heimdall-users-api"`；`IHeimdallCredentialProvider` 提供 ClientId/Secret/Version。`HeimdallClientFactory.CreateUserProvisioning(name).CreateUserAsync(new HeimdallUserCreateRequest { ... })`。
- Node：`HeimdallServerClient` 配置同样边界、`authenticationMethod` 和异步 `credentialProvider`；`new HeimdallUserProvisioningClient(server, apiBaseUrl, tenantId, applicationId).createUser(request)`。只在服务器导入 server 包。
- Directory Client 保持读取职责。浏览器包的实现与 OIDC 登录没有改变；共享版本策略会同步包元数据。

完整可运行配置示例与 OpenAPI 以 [5.6.0 接入指南](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.6.0/docs/backend-user-provisioning.md) 及 [OpenAPI](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.6.0/docs/openapi/backend-user-provisioning.openapi.yaml) 为准。

## 迁移与交付证据

部署前用 `psql -v ON_ERROR_STOP=1 -1` 执行 `be/Database/Migrations/20261017_user_provisioning_default_roles.postgresql.sql`。仅给 `application_role` 和 `tenant_application_role` 新增 `is_user_default boolean NOT NULL DEFAULT false`；不回填管理员角色，不自动为已有 Client 授权。5.6.0 生产迁移已执行。回滚旧镜像保留新增列即可。

版本、提交、镜像 digest 见 [当前交付基线](integration-guide.md#当前交付基线) 和 [v5.6.0 Release](https://github.com/BenLampson/Asgard.Heimdall/releases/tag/v5.6.0)。2026-10-17 完成正式镜像与公开 Node 5.6.0 包的隔离真实 PostgreSQL/Redis 验收：真实 Discovery/JWKS、Client Secret 校验及 Token 签发、连续两次用户创建、当前应用角色/凭据回读、重复 409、角色注入 400。生产冒烟确认接口匿名 401、Scope 被 Discovery 公布、现有登录跳转正常。未为任何业务租户自行新增测试用户或扩权；具体租户接入须另外配置并验收。
