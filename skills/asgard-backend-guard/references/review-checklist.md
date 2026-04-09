# Asgard Backend Review Checklist

## 使用方式

当 `SKILL.md` 已经触发，但你需要更细的逐项复查清单时，再读取本文件。

## 一级检查

1. 这次改动属于哪一层:
   - Controller
   - Service
   - Repository
   - Entity
   - DTO / VO
2. 这次改动是否涉及:
   - CRUD
   - DTO 映射
   - 多租户
   - 审计字段
   - 统一响应
   - 乐观锁

## 乐观锁复查清单

只要看到以下任一线索，就进入重点复查:

- `AbsAsgardBaseEntity`
- `AbsAsgardTenantEntity`
- `AbsAsgardTenantUserDataEntity`
- `Version`
- `[Column(IsVersion = true)]`
- `UpdateAsync(string id, XxxDto dto, ...)`

逐项检查:

1. 是否出现 `dto.ToEntity()` 后直接更新
2. 是否先 `GetByIdAsync(id)` 或等价读取当前实体
3. 是否把 `Version` 当成 DTO/前端可信输入
4. 是否覆盖了 `CreateBy`、`CreateTime`、`Deleted`
5. 是否覆盖了 `TenantId`、`ClientId`
6. 是否优先调用实体行为方法
7. 如果没有行为方法，是否显式逐字段赋值
8. 如果实体约定需要审计更新，是否调用 `MarkAsUpdated()`

## 分层复查清单

### Controller

1. 是否继承 `BaseController`
2. 是否只做输入输出编排
3. 是否返回统一响应壳
4. 是否把 DTO 转成 VO 后再返回

### Service

1. 是否承担业务编排而不是直接写控制器逻辑
2. 是否误把 DTO 重建成新实体再更新
3. 是否把持久化字段开放给外部输入覆盖

### Repository

1. 是否继承 `AbsAsgardRepositoryBase<TEntity, TKey>`
2. 是否误写另一套默认仓储模式
3. 是否重复手写默认租户过滤

## 结论规则

- 会导致并发失败、数据覆盖、租户越权、响应契约破坏的，按高优先级问题报告
- 会导致后续维护高风险的，按中优先级问题报告
- 只是建议优化的，不要和硬问题混在一起
