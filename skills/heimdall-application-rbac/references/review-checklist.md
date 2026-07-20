# 应用域 RBAC Review Checklist

## 服务端授权

- 每个读写入口是否校验 Application 和 Tenant 两个资源维度？
- 宽泛的全局 CRUD Controller 是否能绕过应用范围守卫？
- 应用管理员是否必须同时具有 scoped 原生权限与有效 Application Grant？
- Selected Tenant Grant 是否与相同 application_id 的有效 TenantApplication 做交集？
- AllApplicationTenants 是否只覆盖该应用，且自动包含未来绑定 Tenant？
- 平台超管旁路是否明确、唯一且有审计？
- Tenant 用户是否只能访问 Token tenant_id，且绑定必须 Enabled？
- 未知应用、未知 Client、缺版本或查询失败是否 Fail Closed？

## 可见性与生命周期

- 应用管理员是否看不到未授权、未绑定 Tenant？
- 开通已有 Tenant 是否要求准确 code、邀请或确认，而非模糊搜索？
- 创建新 Tenant 是否额外要求 `platform.tenant.manage`？
- Disabled 是否阻断签发和业务访问但保留管理恢复入口？
- 是否完全没有“解绑后重新部署”的破坏性流程？

## Manifest 与数据完整性

- Manifest null、枚举、重复 code、未知 permission 和角色继承环是否拒绝？
- Manifest 版本是否单调递增并保留历史？
- Tenant applied version 是否只在同步成功后更新？
- 内置角色是否同步，自定义角色是否保留？
- 稳定业务唯一键是否不含 `Deleted`/状态？
- 同 code 恢复是否复用原记录与 ID？
- 批量软删除和关系更新是否递增实体 Version 与授权版本？

## JWT 与 mini issuer

- 一枚 Token 是否只有一个 application_id？
- roles/permissions 是否按 Application/Tenant 裁剪？
- 业务 Token 是否排除全部 `platform.*`？
- 三个版本 Claim 是否使用权威快照且按字符串精确比较？
- `Issue(AbsAsgardUserInfo)` 与直接 Subject 签发是否保留相同应用 Claim？
- mini issuer 是否只签发调用方传入快照，而未自建另一套授权数据库？

## 数据库迁移

- 是否迁移软删除、停用和历史行，而不只迁移活跃数据？
- 新 NOT NULL/外键前是否完成回填和 postcheck？
- OIDC Client 是否全部获得正确 application_id？
- 是否检查重复业务键和孤儿关联？
- 是否提供分阶段执行与失败回滚边界？

## 测试矩阵

- 平台超管、全局管理员、应用管理员、Tenant 管理员、普通用户。
- 正确应用、其他应用；Selected Tenant、其他 Tenant、未来新绑定 Tenant。
- 未绑定、Enabled、Disabled、重新 Enabled。
- 当前版本、旧 Manifest、旧应用授权、旧 Tenant 授权 Token。
- 直接 Subject 签发、AbsAsgardUserInfo 转签、资源端 Claim 解析。
- 后端拒绝用例必须独立存在，不能只依赖前端隐藏菜单。
