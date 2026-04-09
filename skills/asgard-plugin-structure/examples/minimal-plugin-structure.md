# 最小 Asgard 项目结构示例

## 模式 A：单项目快速验证

```text
DemoPlugin/
├── app.yaml
├── plugin.yaml
├── GlobalUsings.cs
├── Program.cs
├── DemoPlugin.csproj
├── Config/
│   └── PluginConfigs/
│       └── DemoPluginConfig.cs
├── Controllers/
├── Mapper/
├── Models/
│   ├── VO/
│   ├── DTO/
│   └── Entities/
├── Domains/
│   ├── IRepositories/
│   └── Repositories/
├── Services/
│   ├── IServices/
│   └── Services/
├── Extensions/
├── Middlewares/
└── wwwroot/
```

## 适用场景

- 新功能 PoC
- 快速验证单个插件
- 临时演示生命周期、配置绑定或调试链路

## 关键点

- 这是快速验证结构，不是正式开发唯一标准
- `Program.cs` 只是在该模式下与插件代码同项目
- `plugin.yaml` 属于插件清单
- `app.yaml` 作为运行配置入口，直接由当前项目加载
