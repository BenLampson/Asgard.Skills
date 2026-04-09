---
name: asgard-backend-guard
description: Asgard 后端代码复查与守卫 skill。Use when reviewing or self-checking Asgard backend changes, especially for Controller/Service/Repository/Entity code, DTO mapping, CRUD flows, tenant data, optimistic-lock updates, response wrappers, audit fields, or other places where generated code may violate Asgard hard rules or repeat known project pitfalls.
---

# Asgard Backend Guard

## 作用

本 skill 用于在 Asgard 后端代码生成后、修改后、提交前做一次高优先级复查，目标不是重新设计业务，而是尽快识别：

- 是否违反 Asgard 硬规则
- 是否出现项目里已经反复踩过的坑
- 是否存在“代码能跑，但以后一定出事”的高风险实现

这个 skill 是审查型 skill，不负责替代 `$asgard-dotnet-10-csharp-14`、`$asgard-database`、`$asgard-api-development` 等开发规范；它负责在实现完成后做守门和兜底。

## 什么时候必须使用

出现以下任一情况时，优先启用本 skill 做复查：

- 用户要求“review / 复查 / 检查一下 / 看看有没有坑”
- 刚新增或修改了 `Controller / Service / Repository / Entity / DTO / VO`
- 刚实现了 `Create / Update / Delete / Enable / Disable / Patch`
- 代码里出现 `dto.ToEntity()`、`UpdateAsync(...)`、`GetByIdAsync(...)`
- 代码涉及 `TenantId`、`CreateBy`、`CreateTime`、`Deleted`、`Version`
- 代码涉及统一响应壳、租户边界、仓储基类、实体基类
- 你准备生成或修改 Asgard 后端 CRUD 代码，但还不确定是否踩中了项目坑点

如果既要“写代码”又要“复查代码”，先按对应开发 skill 完成实现，再用本 skill 复查。

## 复查流程

按下面顺序复查，避免只看表面：

1. 先识别变更落在哪一层：`Controller -> Service -> Repository -> Entity`
2. 再识别有没有触发 Asgard 硬规则：统一响应、分层边界、租户边界、乐观锁、审计字段
3. 再找已知高风险模式：DTO 重建实体更新、前端覆盖持久化字段、越层访问、漏掉包装等
4. 最后判断问题级别：
   - 会直接导致错误、异常、并发失败、数据污染：按高优先级指出
   - 目前可运行但后续极易踩坑：按中优先级指出
   - 只是风格或建议：放在次级说明，不要盖过真正风险

## 核心检查项

### 1. 乐观锁更新检查

这是 Asgard 当前最重要的复查项，必须优先检查。

只要满足以下任一条件，就必须把实体按乐观锁实体处理：

- 实体继承 `AbsAsgardBaseEntity`
- 实体继承 `AbsAsgardTenantEntity`
- 实体继承 `AbsAsgardTenantUserDataEntity`
- 实体存在 `Version` 字段
- 实体带 `[Column(IsVersion = true)]`

复查结论必须遵守以下硬规则：

- `Create` 场景可以使用 `dto.ToEntity()`
- `Update` 场景默认禁止使用 `dto.ToEntity()` 后直接 `UpdateAsync(entity)`
- 更新时必须先从数据库读取当前实体，再在原实体上应用允许修改的字段
- `Version` 只能来自数据库当前实体，不能信任 DTO 或前端
- 不允许 DTO 覆盖 `CreateTime`、`CreateBy`、`Deleted`、`TenantId`、`ClientId` 等持久化字段
- 对租户实体，不允许在更新时随 DTO 覆盖租户归属字段，除非业务明确允许且有中文注释说明
- 如果实体有 `Update(...)`、`Enable()`、`Disable()` 等行为方法，优先调用实体方法
- 如果实体没有行为方法，再显式逐字段赋值，并在约定需要时调用 `MarkAsUpdated()`

必须重点警惕以下签名：

```csharp
Task UpdateAsync(string id, XxxDto dto, ...)
```

只要看到这种签名，就主动检查是否存在“DTO 重建实体后直接更新”的错误路径。

反模式：

```csharp
var entity = dto.ToEntity();
entity.Id = id;
await repository.UpdateAsync(entity);
```

推荐模式：

```csharp
var entity = await repository.GetByIdAsync(id)
    ?? throw new InvalidOperationException($"未找到实体：{id}");

entity.Update(...);
await repository.UpdateAsync(entity);
```

### 2. 分层边界检查

- Controller 只能做输入输出编排，不能直接写仓储或拼 ORM 查询
- Service 负责业务编排，不要直接返回给前端的响应壳模型
- Repository 只做数据访问，不要塞业务判断
- Entity 负责自己的状态行为时，优先把状态变更放在实体方法中

### 3. 统一响应检查

- Controller 必须继承 `BaseController`
- 对外返回必须使用 `Response<T>`、`Response<object>`、`PageResponse<T>` 或 `CursorResponse<T>`
- 不要直接返回裸 DTO、VO、集合、字符串、布尔值、数字、匿名对象

### 4. 租户与审计字段检查

- 租户默认依赖框架全局过滤与身份上下文，不要到处手写默认 `TenantId` 过滤
- 不要让前端输入决定 `TenantId`、`CreateBy`、`CreateTime`、`Deleted`
- 后台服务或平台级特例如果允许跨租户，必须在代码中写清楚边界和原因

## 输出要求

当本 skill 用于 review 时：

- 先给“问题清单”，不要先写长篇总结
- 优先报告 bug、风险、行为回归、缺失校验、缺失测试
- 每条问题都尽量指出具体文件、方法、模式或触发原因
- 如果没有发现问题，要明确写“未发现明确问题”，并补一句剩余风险或未验证项

## 参考导航

- 编码硬规则看 `$asgard-dotnet-10-csharp-14`
- 数据访问与乐观锁规则看 `$asgard-database`
- Controller 统一响应规则看 `$asgard-api-development`
- 仓储与服务职责边界看 `$asgard-repository-service-registration`
- 需要更详细复查清单时，读取 `references/review-checklist.md`
