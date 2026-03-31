# Asgard 插件标准流转

## 输出链路

```text
数据库
   ↓
Entity  【查询】
   ↓
Service → 转 DTO  【业务处理】
   ↓
Controller → 转 VO 【给前端展示】
   ↓
前端页面
```

- 实际代码访问链路中，`Service` 必须通过 `Repository` 访问 `Entity` 与数据库
- 这条输出链路强调的是模型转换职责，不表示可以跳过仓储层

## 进入链路

```text
前端请求
   ↓
Controller
   ↓
Service
   ↓
Repository
   ↓
Entity
   ↓
数据库
```

## 层级职责

- `Entity`
  表示数据库原始对象
- `Repository`
  负责实体读写与查询
- `Service`
  负责业务编排，并把实体处理成 DTO
- `Controller`
  负责接口入口，并把 DTO 进一步转为 VO
- `VO`
  只用于前端展示
- `DTO`
  只用于传输和业务编排

## 不要这样做

- 不要让 Controller 直接操作 Entity
- 不要让 Repository 直接返回 VO
- 不要把展示拼装逻辑放进 Entity
- 不要把 Mapper 逻辑散落在多个层里重复实现
