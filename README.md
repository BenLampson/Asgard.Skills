# Asgard Skills

Asgard 框架的 AI 技能仓库。

## 概述

这个仓库专门存放 Asgard 框架开发相关的 AI 技能定义，用于：

- 为 AI 编码助手提供框架特定的技能支持
- 标准化 Asgard 框架各模块的开发规范
- 集中管理和版本控制技能定义文件
- 方便在不同开发环境间同步技能配置

## 技能结构

每个技能通常包含：

- `SKILL.md` - 技能描述和使用说明
- `agents/` - AI 代理配置文件
- `references/` - 从主仓库同步的关键源码参考
- `templates/` - 推荐代码模板或落地片段
- 其他相关资源文件

## 使用方式

本仓库作为 Asgard 主项目的子目录，由 AI 助手自动加载使用。技能定义会根据 Asgard 框架的演进持续更新。

当前需要重点遵守的一条 API 硬规则是：

- 所有 Asgard Controller 都必须继承 `BaseController`
- 分层职责固定为：`Controller -> Service -> Repository -> Entity`
- 输出职责固定为：`Service` 产出 DTO，`Controller` 把 DTO 转成 VO 后，再统一包装成 `Response<T>`、`Response<object>`、`PageResponse<T>` 或 `CursorResponse<T>`
- 不允许 Controller 直接返回未包装的 DTO / VO / 集合 / 基元 / 匿名对象

当前还需要重点遵守的一条身份硬规则是：

- Asgard 的统一用户信息模型必须建立在 `AbsAsgardUserInfo` 之上
- IDP、认证测试、授权链路都必须复用同一套标准 claims 契约
- 不允许在不同项目、不同插件、不同测试里各自发明“用户信息 JSON”或随意命名 claims

Heimdall 与微服务集成时，使用 `heimdall-service-integration`：

- BackendService 目录读取必须同时限制 Audience、`token_type`、Scope 和 Token 租户
- `TenantUser.Id` 必须与 JWT `sub`、Webhook `subject_id` 和目录 `tenant_user_id` 保持一致
- 停用与删除通过事务 Outbox 投递身份失效事件，下游使用短 Token、撤销水位、短缓存和对账 Fail Closed

当前还需要重点了解的一条工具约定是：

- TypeScript 客户端方案由项目自行选择；`Asgard.TsGen` 是可选的官方生成方案
- 只有项目选择 TsGen 且 Controller 标记了 `[AsgardTsGen]` 时，控制器才会进入生成结果
- 默认输出目录就是命令执行时的当前目录
- 生成器会重建 `common/`、`controller/`、`models/` 这类纯生成目录，因此这些目录不应手写自定义代码

当前还需要重点使用的一条复查约定是：

- Asgard 后端代码在生成后、修改后、提交前，优先使用 `asgard-backend-guard` 做一次复查
- 该 skill 专门检查后端硬规则、分层边界、统一响应、租户与审计字段、乐观锁更新等高频踩坑点
- 遇到 `UpdateAsync(string id, XxxDto dto, ...)`、`dto.ToEntity()`、`Version`、`TenantId` 等线索时，应主动启用该 skill 做风险排查

## 仓库边界

`Asgard Skills` 作为独立维护的 Git 仓库存在，日常可按 Asgard 主项目的子模块 / 子仓库方式接入。

- 技能内容的版本状态、提交与历史，应优先在当前目录对应的 Git 仓库内查看
- 上层 Asgard 仓库看到的目录状态，不代表 `Asgard Skills` 内部文件没有被单独版本控制
- 修改技能定义时，应明确这是在维护 `Asgard Skills` 自身，而不是直接修改 Asgard 主仓库普通目录

如果需要查看本仓库状态，请在 `src/Asgard Skills` 目录内执行 Git 命令。

## 许可证

MIT
