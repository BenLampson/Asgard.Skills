# Heimdall 应用域契约

## 关系模型

```text
ApplicationInfo
  ├─ ApplicationManifest (version)
  ├─ ApplicationPermission (stable application_id + code)
  ├─ ApplicationRole template
  │    └─ ApplicationRolePermission -> ApplicationPermission
  ├─ TenantApplication (tenant_id + application_id, status, applied_manifest_version)
  │    ├─ TenantApplicationRole (built-in or custom)
  │    ├─ TenantApplicationRolePermission -> ApplicationPermission
  │    └─ TenantUserApplicationRole
  └─ SysUserApplicationGrant
       └─ SysUserApplicationTenantGrant (when tenant scope is selected)
```

权限是应用级权威定义，角色是权限组合。Manifest 发布产生新定义版本；Tenant 同步只更新内置角色实例，自定义角色保持 Tenant 自治。

## 原生平台权限

应用管理员角色应只包含：

- `platform.application.tenant.manage`
- `platform.application.tenant_rbac.manage`
- `platform.application.oidc_client.manage`

不得隐式包含：

- `platform.application.manage`
- `platform.tenant.manage`
- `platform.tenant_rbac.manage`
- `platform.oidc_client.manage`

Manifest 发布和应用管理员授权仅由平台超管执行。应用目录的普通维护权限不代表能发布安全边界定义。

## 授权公式

平台超管：显式放行，但仍以请求指定资源执行，所有操作保留审计。

应用管理员访问某 Tenant：

```text
具有对应 application-scoped 原生权限
AND 存在有效 SysUserApplicationGrant(application_id)
AND (
  grant.scope = AllApplicationTenants
  OR 存在有效 SysUserApplicationTenantGrant(application_id, tenant_id)
)
AND TenantApplication(application_id, tenant_id) 关系存在
```

`AllApplicationTenants` 自动包含未来新绑定 Tenant，但不能跨 Application。`SelectedTenants` 只允许显式 Tenant；开通新的已有 Tenant 时，只扩展实际授权本次操作的那一条 Grant，不能扩展该用户全部 Grant。

Tenant 用户：Token 的 `tenant_id` 必须与路由资源一致，TenantApplication 必须为 Enabled，并具有目标应用下的 Tenant workspace 权限。

## 开通与状态

开通时显式二选一：

- 绑定已有 Tenant：输入准确 Tenant Code、使用邀请，或由 Tenant/Heimdall 有权管理员确认。应用管理员不能搜索全公司 Tenant。
- 创建新 Tenant：除应用管理授权外，还必须具有 `platform.tenant.manage`。

状态语义：

```text
不存在 -> Pending/Enabled -> Disabled -> Enabled
```

不提供解绑或物理删除。Disabled 阻断 Token 签发、Tenant 用户访问和业务使用；管理端仍可看见关系并重新启用，历史 RBAC、Client 与审计数据保留。

## Manifest 同步与版本

- Application Manifest 版本单调递增且不可覆盖历史版本。
- Permission 的稳定业务键是 `(application_id, code)`，生命周期状态不参与唯一键。
- 同 code 的软删除 Permission 再次出现时恢复同一记录，避免旧角色关系悬挂。
- TenantApplication 保存实际成功同步的 Manifest 版本。
- 内置角色按 Manifest 同步；Tenant 自定义角色不被覆盖。
- `application_authorization_version` 在应用级授权关系变化时更新。
- `tenant_authorization_version` 在目标应用与 Tenant 的角色、权限、用户或外部映射变化时更新。

版本在 JWT 中按不透明字符串传播。资源方只做精确相等判断；不使用大小比较猜测新旧。

## Token 与 SSO

应用 Token 至少包含：

- `application_id`
- `application_manifest_version`
- `application_authorization_version`
- Tenant 上下文时的 `tenant_authorization_version`
- 已按 Application/Tenant 裁剪的 `roles` 与 `permissions`

业务应用 Token 不得包含 `platform.*`。从应用 A 进入 B 时为 B 重新执行授权码流程并签发 B Token；Heimdall SSO Cookie 有效时通常不显示登录页。不能允许用任意 A Token 无条件换 B Token。

mini issuer 可以输出同一 Claim 合约，但它不是授权中心。调用方负责从权威来源取得已裁剪快照和版本；无法确认时不签发。

## 迁移顺序

1. 预检重复业务键、孤儿关系、无应用归属 Client 和非法状态。
2. 创建系统内置 Heimdall Application 和目标表结构。
3. 回填所有关系的 application_id，包括软删除和历史 OIDC Client。
4. 建立稳定业务唯一索引和外键。
5. 发布初始 Manifest、实例化 Tenant 内置角色并记录 applied version。
6. 做正反向数量与孤儿检查，再收紧 NOT NULL。
7. 只有在回滚窗口结束后才清理过渡字段。

迁移必须幂等、可审计，并为 precheck、postcheck、cleanup 分阶段执行。
