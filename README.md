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

`skills/` 是维护与分发目录，放在主项目子目录中不会自动成为 Codex 可用技能。需安装到当前环境的技能发现目录，或通过插件分发。

Windows 本机安装（默认链接到 `~/.agents/skills`，不复制文件，仓库更新即时反映）：

```powershell
./scripts/install-skills.ps1
```

已使用 `~/.codex/skills` 的环境可显式指定现有目录，避免安装两份同名技能：

```powershell
./scripts/install-skills.ps1 -Destination "$env:USERPROFILE/.codex/skills"
```

团队按项目使用时，可指定项目的 `.agents/skills` 目录。安装脚本会补齐全部技能，并拒绝覆盖其他来源的同名目录。安装后检查可用技能列表；更新没有出现时重启 Codex。

提交前校验（需要 Python 和 PyYAML）：

```powershell
python -X utf8 scripts/validate_skills.py
```

description 的仓库维护上限为 180 字符，触发条件和边界前置。这个上限是本仓库约定，不是平台限制。长示例保存在 references，入口写明读取场景；静态校验通过不代表自动触发率已经验证。

缓存入口与新模板面向 Asgard 5.3+；含旧缓存接口的源码快照显式标记历史版本。任务先核对目标项目依赖版本，不混用接口，也不自动升级项目。

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

Heimdall 应用权限与 Tenant 绑定设计、实现和 review 时，使用 `heimdall-application-rbac`：

- 固化 Application Manifest、权限模板、TenantApplication 和 SystemUser Application Grant 的关系
- 区分全局平台权限与应用范围管理权限，应用管理员只能访问授权应用与 Tenant
- 约束 Manifest/授权版本 Claim、停用而非解绑、稳定唯一键与软删除恢复
- 同时覆盖完整版 Heimdall 和 mini JWT issuer 的应用 Claim 合约边界

Heimdall MCP 管理能力开发、集成和 review 时，使用 `heimdall-mcp-management`：

- 覆盖 `/mcp` Streamable HTTP、OAuth Bearer 与 AK/SK 双认证
- 约束平台/租户工具、Resources、Prompts、Tasks 和二阶段写确认
- 约束凭据工具/权限/CIDR/有效期/速率/并发策略、租户边界和安全审计
- 强制薄封装已有 Service，禁止在 MCP 中复制业务逻辑

当前还需要重点了解的一条工具约定是：

- TypeScript 客户端方案由项目自行选择；`Asgard.TsGen` 是可选的官方生成方案
- 只有项目选择 TsGen 且 Controller 标记了 `[AsgardTsGen]` 时，控制器才会进入生成结果
- 默认输出目录就是命令执行时的当前目录
- 生成器会重建 `common/`、`controller/`、`models/` 这类纯生成目录，因此这些目录不应手写自定义代码

当前还需要重点使用的一条复查约定是：

- Asgard 后端代码在生成后、修改后、提交前，优先使用 `asgard-backend-guard` 做一次复查
- 该 skill 专门检查后端硬规则、分层边界、统一响应、租户与审计字段、乐观锁更新等高频踩坑点
- 遇到 `UpdateAsync(string id, XxxDto dto, ...)`、`dto.ToEntity()`、`Version`、`TenantId` 等线索时，应主动启用该 skill 做风险排查

Asgard Redis 分布式锁的配置、注册、自动续租和安全使用统一使用 `asgard-distributed-lock`：

- Yggdrasil 在缓存与 Redis 同时启用时自动装配 `IDistributedLock`
- `distributedLock` 配置节可省略，且没有独立启用开关
- 长时间任务必须监听 `IDistributedLockHandle.LockLostToken`
- 释放与续租必须保持 owner token 原子校验语义

## 仓库边界

`Asgard Skills` 作为独立维护的 Git 仓库存在，日常可按 Asgard 主项目的子模块 / 子仓库方式接入。

- 技能内容的版本状态、提交与历史，应优先在当前目录对应的 Git 仓库内查看
- 上层 Asgard 仓库看到的目录状态，不代表 `Asgard Skills` 内部文件没有被单独版本控制
- 修改技能定义时，应明确这是在维护 `Asgard Skills` 自身，而不是直接修改 Asgard 主仓库普通目录

如果需要查看本仓库状态，请在 `src/Asgard Skills` 目录内执行 Git 命令。

## 许可证

MIT
