# Tenant BackendService Client 生命周期

## 创建原则

- 每个 Tenant 使用独立的 Confidential Client。
- 只授予所需 Grant 和 Scope；目录读取只需要 `client_credentials` 与 `heimdall.directory.read`。
- Client 不得绑定多个 Tenant，也不得获得目录写权限。
- 生产环境只通过 TLS 调用管理与 Token Endpoint。

## 当前创建方式

管理接口：

```http
POST /api/TenantOidcClient
```

创建请求至少需要 Tenant、Client 名称、Confidential Client 类型、`client_credentials`、Scope、Token Endpoint 认证方式和强随机 Secret。Client ID 由 Heimdall 生成；当前创建接口由调用方生成并提交 Secret，Heimdall 只保存哈希，普通查询和创建响应不会返回明文 Secret。

因此当前安全流程是：

1. 开通方在受控环境生成高强度随机 Secret。
2. 通过 TLS 创建 Client。
3. 同一流程将 Client ID 与 Secret 写入下游 Secret Manager。
4. 不在脚本输出、CI 日志或业务数据库中保留明文。

若以后改成服务端生成并一次性返回 Secret，应把它作为外部契约变更，同步更新实现、测试和本文档。

人工开通也可使用：

```http
POST /api/Tenant/onboarding
Idempotency-Key: <stable-key>
```

该接口原子创建 Tenant、内建 Scope/Role、Client、签名密钥和管理员，但当前同样要求调用方提供 Client Secret，响应不回显明文。

## 申请 Token

使用标准 Client Credentials 请求：

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=<client-id>&client_secret=<secret>&scope=heimdall.directory.read
```

接入方必须验证签发结果具备：

```text
token_type = BackendService
tenant_id = 该 Client 绑定的 Tenant
scope      = heimdall.directory.read
aud        = heimdall-directory-api
```

不要只以“成功拿到 JWT”作为验收结果。

## 保存与使用

- 按环境和 Tenant 分层保存，例如 `heimdall/<environment>/<tenant-id>/directory-client`。
- Secret 只能由负责取 Token 的服务读取。
- 日志允许记录 Tenant ID、Client ID、Key ID 和请求追踪号，不记录 Secret 或完整 Token。
- Token 在内存中按过期时间缓存，预留刷新抖动，避免每次目录请求都访问 Token Endpoint。
- 取 Token 失败时，不使用其他 Tenant 的 Client 兜底。

## 轮换

接口：

```http
POST /api/TenantOidcClient/{id}/reset-secret
```

请求支持 `GracePeriodMinutes`，范围 0–1440，默认 60。响应中的 `NewClientSecret` 只在本次返回；随后无法再次读取。非零宽限期允许旧、新 Secret 短暂并行，0 表示旧 Secret 立即失效。

推荐顺序：

1. 发起轮换并选择足够覆盖部署的宽限期。
2. 立即把新 Secret 写入 Secret Manager。
3. 滚动更新取 Token 服务。
4. 用新 Secret 实际完成一次 Token 和目录 API 调用。
5. 确认所有实例已切换，等待旧 Secret 到期；高风险事件使用 0 分钟立即切断。

若一次性响应丢失，不尝试读取当前 Secret，直接再次轮换。

## 停用与吊销

```http
POST   /api/TenantOidcClient/{id}/status
DELETE /api/TenantOidcClient/{id}
```

疑似泄露时先停用或立即轮换，再调查日志。删除用于确定不再使用的 Client，不应作为常规轮换手段。停用 Client 后还要根据安全策略处理已经签发但尚未过期的 Access Token。
