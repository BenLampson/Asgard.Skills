# BackendService 只读目录 API

## 目录

- [安全契约](#安全契约)
- [已交付路由](#已交付路由)
- [响应模型](#响应模型)
- [单用户查询](#单用户查询)
- [状态与时间字段](#状态与时间字段)
- [调用策略](#调用策略)
- [禁止事项](#禁止事项)

## 安全契约

每条路由都要求：

| 检查 | 值 |
|---|---|
| Token type | `BackendService` |
| Scope | `heimdall.directory.read` |
| Audience | `heimdall-directory-api` |
| Tenant 来源 | 已验证 Token 的 `tenant_id` |

接口不接受调用方通过 Route、Query 或 Body 指定 Tenant。ID 属于其他 Tenant 时按不存在处理，避免跨租户枚举。

## 已交付路由

```http
GET /api/backend/directory/users?page=1&size=100
GET /api/backend/directory/users/{tenantUserId}
GET /api/backend/directory/groups/{groupId}
GET /api/backend/directory/groups/{groupId}/members?page=1&size=100
GET /api/backend/directory/groups/{groupId}/members/{tenantUserId}
```

分页参数为 `page >= 1`，`size` 默认 10、最大 500。列表使用统一分页响应，详情使用统一响应模型。

### 用户分页

用于定时全租户身份对账。用户对象的 `id` 是规范 `TenantUser.Id`，并提供基础展示资料、最终启停状态和稳定 `updated_at`。

### 组详情

用于确认组是否存在、未删除且启用。不要只以 HTTP 200 推断组可用于路由，必须读取状态字段。

### 组成员分页

用于获得候选集合。成员对象应包含用户最终状态和成员关系的有效性，调用方只使用有效成员。

### 单成员校验

用于自动路由逐候选人确认。有效成员必须同时满足：

- 成员关系存在且未删除；
- DirectoryGroup 存在、未删除且启用；
- TenantUser 存在、未删除且最终状态启用。

关系不存在时返回 `active=false`；用户或组不存在时返回 404。调用方不能把错误、超时或无法解析响应当成有效成员。

## 响应模型

所有响应包含 `code` 和 `message`。分页响应另外包含 `data`、`totalCount`、`page` 和 `size`。

用户分页示例：

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "id": "tenant-user-id",
      "tenant_id": "tenant-id",
      "username": "agent01",
      "display_name": "Agent 01",
      "email": "agent01@example.com",
      "phone": null,
      "avatar_url": null,
      "status": 1,
      "updated_at": "2026-07-18T10:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "size": 100
}
```

目录组对象字段：

```text
id, tenant_id, code, name, status, member_count, updated_at
```

成员分页和单成员校验使用相同元素：

```json
{
  "tenant_user_id": "tenant-user-id",
  "directory_group_id": "support-group-id",
  "active": true,
  "updated_at": "2026-07-18T10:00:00Z"
}
```

机器可读契约以 Heimdall 仓库的 `docs/openapi/backend-directory-api.openapi.yaml` 为准。修改字段或路由时必须同步更新该文件与本 reference。

## 单用户查询

```http
GET /api/backend/directory/users/{tenantUserId}
```

该路由返回与用户分页元素相同的对象，用于创建或重新启用业务 Profile 时确认用户存在且最终状态 Enabled，尤其适用于没有指定主目录组的场景。不存在、已删除或属于其他 Tenant 的用户统一返回 `404`。

该路由自 Heimdall `5.1.1` 和 OpenAPI `1.1.0` 起正式交付，并保留在 `5.1.2`。调用方应固定正式镜像 digest，不得通过遍历分页或跳过校验代替。

## 状态与时间字段

- 用户和组状态当前使用 `0 = Disabled`、`1 = Enabled`。
- TenantUser 最终启用要求用户未删除，且至少存在一条未删除、启用的登录记录。
- `updated_at` 必须稳定反映影响响应语义的变化，可用于短 TTL 缓存再验证。
- 若未来提供 ETag，应继续保留清晰的缓存失效语义并更新 OpenAPI。

## 调用策略

- 自动路由：先确认组有效，再对最终候选人进行有效成员校验。
- Profile 创建/启用：使用单用户接口；调用失败或身份状态不确定时 Fail Closed。
- 定时对账：分页拉取全部用户，处理分页期间的数据变化与重复项。
- 缓存：仅使用短 TTL；状态敏感操作可绕过缓存或强制刷新。
- 故障：401/403 视为配置或凭据错误，404 视为租户内不存在，429/5xx/超时可有限重试，但最终仍 Fail Closed。

正式 Docker 配置默认使用宿主统一限流：每个限流分区 60 秒 300 个请求。部署调整 `host.rateLimiting.permitLimit` 或 `windowSeconds` 时，必须同步更新环境交付记录。

Heimdall 仓库提供 `docs/scripts/provision-backend-directory-fixture.ps1`，用于通过管理 API 创建独立联调 Tenant、两个启用用户、目录组和 Tenant-bound BackendService Client。脚本生成的 Secret 只在结果中返回一次，不得写入仓库、日志或业务表。

## 禁止事项

- 不调用前端管理员 API 代替 BackendService API。
- 不直接查询 Heimdall 数据库。
- 不信任浏览器提交的 Tenant、组或成员关系。
- 不给 BackendService 组织写权限。
- 不使用其他 Tenant 的 Client 或缓存结果兜底。
