# Asgard Skills 仓库维护

- 本仓库维护技能，不是 Asgard 应用代码仓库。编辑技能时使用 `$skill-creator`；不要因示例出现 C# 就启用所有开发 skill。
- `skills/*/SKILL.md` 的 description 是自动发现入口：前置用途与边界，保持简短。正文前部保留核心约束，长示例按场景链接到 references。
- Asgard C# 编码规则的唯一来源是 `asgard-dotnet-10-csharp-14`。修改模块规则时保持与它一致，不在其他技能另建冲突权威。
- 当前缓存指导和新模板面向 5.3+；旧源码快照必须标注历史版本。维护旧项目时先核对项目版本，不自动升级依赖。
- 修改公共接口时检查其他技能、参考文件和模板中的调用，不能只更新一个入口。
- 提交前运行 `python -X utf8 scripts/validate_skills.py`；修改可执行模板时验证生成代码能够编译。安装脚本不得覆盖非本仓库的已有技能。
