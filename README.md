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

## 仓库边界

`Asgard Skills` 作为独立维护的 Git 仓库存在，日常可按 Asgard 主项目的子模块 / 子仓库方式接入。

- 技能内容的版本状态、提交与历史，应优先在当前目录对应的 Git 仓库内查看
- 上层 Asgard 仓库看到的目录状态，不代表 `Asgard Skills` 内部文件没有被单独版本控制
- 修改技能定义时，应明确这是在维护 `Asgard Skills` 自身，而不是直接修改 Asgard 主仓库普通目录

如果需要查看本仓库状态，请在 `src/Asgard Skills` 目录内执行 Git 命令。

## 许可证

MIT
