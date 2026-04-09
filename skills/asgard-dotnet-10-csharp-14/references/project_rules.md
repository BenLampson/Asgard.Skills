# 项目文件要求

- 文件编码格式强制按照

```
{
    "files.encoding": "utf8",
    "files.eol": "\r\n",
    "files.autoGuessEncoding": false
}
```

# 必须遵循的要求

1. 对于 **public** 类型的任何内容，以及任何形式的字段 & 属性，注释覆盖率不能低于 80%
2. 注释必须使用中文
3. 代码必须符合 C# 14 代码规范,包括命名规范,注释规范,空格规范等.系统给你提供了C# 14的Skill,你需要使用这个Skill来编写代码.
4. 注释要写好,这个内容是否有对应的单元测试,如果有,必须要写好对应的单元测试
5. 修改了代码,如果有对应的单元测试,必须要更新对应的单元测试
6. 修改了代码必须要修改对应的注释
7. 写完代码要编译一遍,解决一切警告和错误,包括但不限于编译错误,警告,注释错误等.
8. 使用 "XXXXException.ThrowIfNull"，而不是显式引发新的异常实例

# 项目要求

项目利用Global using来引入必要的命名空间,避免在每个文件中都引入命名空间.

# Asgard 默认更新策略

以下规则属于必须遵循的硬约束, AI 在生成服务层/仓储层更新代码时必须默认套用:

1. 对于继承 `AbsAsgardBaseEntity`、`AbsAsgardTenantEntity`、`AbsAsgardTenantUserDataEntity` 的实体, 更新时禁止使用 `dto.ToEntity()` 后直接 `UpdateAsync(entity)`
2. 只要实体存在 `Version` 字段或 `[Column(IsVersion = true)]`, 就必须视为启用了 FreeSql 乐观锁, 默认采用“先查后改”
3. Update 场景必须先从数据库读取当前实体, 再在原实体上应用 DTO 中允许修改的字段, 最后执行更新
4. Create 场景可以使用 `dto.ToEntity()`, 但 Update 场景默认不能这样持久化
5. DTO 不是 `Version` 的可信来源, 乐观锁版本必须来自数据库当前实体
6. 不允许让前端或 DTO 决定 `CreateTime`、`CreateBy`、`Deleted`、`TenantId`、`ClientId` 等持久化字段
7. 对租户实体, 更新时不允许随 DTO 覆盖租户归属字段, 除非业务明确允许且代码中有中文注释说明
8. 如果实体提供 `Update(...)`、`Enable()`、`Disable()` 等行为方法, 优先调用实体方法; 如果没有, 再显式逐字段赋值, 并在约定需要时调用 `MarkAsUpdated()`
9. 遇到 `UpdateAsync(string id, XxxDto dto, ...)` 这类签名时, 必须优先检查是否存在乐观锁实体, 不要生成“DTO 重建实体后直接更新”的代码
