# 最小 Asgard 插件结构示例

## 目录树

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

- 新建插件项目
- 先搭 starter，再补业务代码
- 需要统一 `GlobalUsings.cs` 与 YAML 根文件位置

## 关键点

- `GlobalUsings.cs` 不是可选项
- `app.yaml` 与 `plugin.yaml` 都在项目根目录
- 空目录可以先留空，但目录名字不要自行改造
