# 可信 JWT 签发方（Asgard 8.2 起）

一个 Heimdall 登录身份可以在业务授权范围内切换企业或账套。沿用原有的宿主 JWT 配置，由业务服务按本次请求校验企业和资源权限。需要同一资源 API 接受独立登录系统的令牌，或登记一个固定 issuer 时，可以使用 `trustedIssuers`。

## 宿主配置

在部署的 `app.yaml` 或等效配置源登记签发方。把示例中的 `issuer` 换成各自 discovery 公布的完整地址，`audience` 填对应的 API 受众，`tenantId` 填令牌应携带的身份租户 ID。

```yaml
host:
  auth:
    enabled: true
    jwt:
      requireHttpsMetadata: true
      trustedIssuers:
        - issuer: "https://shared.example.com/oidc/forseti/"
          audience: "forseti-api"
          tenantId: "11111111-1111-1111-1111-111111111111"
        - issuer: "https://dedicated.example.com/identity/"
          audience: "forseti-api-dedicated"
          tenantId: "22222222-2222-2222-2222-222222222222"
```

- 切换到该模式时移除原 `issuerTemplate` 和全局 `audience`，包括由环境配置提供的旧模板。空列表、重复 issuer 或同时存在模板都会使启动失败。省略列表时沿用模板和全局受众。
- 令牌的 `iss`、discovery 公布的 `issuer` 和登记地址须精确一致，末尾斜杠也要一致。宿主按 `iss` 选登记，再检查该条目的 `audience`。两者匹配后才请求外部元数据。
- 宿主按 issuer 隔离 discovery、JWKS 和换钥状态。JWKS 可部署在独立域名，元数据请求遵守 `requireHttpsMetadata` 并拒绝重定向。
- 验签通过后要求唯一的 `tenant_id` 与登记一致。GUID 按值比较，其他标识按原文比较。应用的企业 ID、账套 ID 和成员资格由业务服务解析和校验。

## 迁移自定义验签

同步升级项目使用的 Asgard 包，并从现有部署登记生成 `trustedIssuers`。让宿主接管签发方选择、验签与 JWKS 刷新。应用特有的 `application_id`、`client_id` 和 `token_type` 检查可接在宿主 `OnTokenValidated` 成功之后。已有算法限制和时钟容差可通过 `JwtBearerOptions.TokenValidationParameters` 继续配置。

联调验证每个签发方的合法令牌、受众和租户匹配，以及单独换钥。未登记的 `iss` 与受众不匹配的令牌应在出站发现请求之前拒绝。切换企业时，使用同一身份验证对目标企业和资源的权限。
