# Asgard Skills 仓库维护

- 本仓库维护技能，不是 Asgard 应用代码仓库。编辑技能时使用 `$skill-creator`；不要因示例出现 C# 就启用所有开发 skill。
- `skills/*/SKILL.md` 的 description 是自动发现入口：前置用途与边界，保持简短。正文前部保留核心约束，长示例按场景链接到 references。
- Asgard C# 编码规则的唯一来源是 `asgard-dotnet-11-csharp-15`。修改模块规则时保持与它一致，不在其他技能另建冲突权威。
- 开发规范、示例和模板统一面向 Asgard 7.x、.NET 11 和 C# 15。旧版本只用于说明如何迁移到当前基线；标记历史源码快照，不把其中的旧接口作为开发示例。
- 修改公共接口时检查其他技能、参考文件和模板中的调用，不能只更新一个入口。
- 提交前运行 `python -X utf8 scripts/validate_skills.py`；修改可执行模板时验证生成代码能够编译。安装脚本不得覆盖非本仓库的已有技能。
