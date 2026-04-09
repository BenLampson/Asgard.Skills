# Asgard 项目结构说明

## 推荐结论

- 正式开发优先采用“插件主体项目 + starter 项目分离”
- 单项目结构只作为快速验证模式存在
- 不要默认把 `Program.cs`、`app.yaml`、`plugin.yaml` 都说成插件项目根目录标配

## 模式 A：单项目快速验证

```text
{ProjectName}/
├── app.yaml
├── plugin.yaml
├── GlobalUsings.cs
├── Program.cs
├── {ProjectName}.csproj
├── Config/
├── wwwroot/
├── Controllers/
├── Mapper/
├── Models/
├── Domains/
├── Services/
├── Extensions/
└── Middlewares/
```

### 适用场景

- 快速验证
- Demo
- 临时调试

### 结构边界

- `Program.cs` 在这个模式下可以与插件代码同项目
- `plugin.yaml` 位于当前项目
- `app.yaml` 由当前项目直接加载

## 模式 B：插件项目 + starter 项目分离

```text
{SolutionRoot}/
├── src/
│   ├── {PluginProjectName}/
│   │   ├── plugin.yaml
│   │   ├── GlobalUsings.cs
│   │   ├── {PluginProjectName}.csproj
│   │   ├── {PluginClassName}.cs
│   │   ├── Config/
│   │   ├── wwwroot/
│   │   ├── Controllers/
│   │   ├── Mapper/
│   │   ├── Models/
│   │   ├── Domains/
│   │   ├── Services/
│   │   ├── Extensions/
│   │   └── Middlewares/
│   └── {StarterProjectName}/
│       ├── app.yaml
│       ├── GlobalUsings.cs
│       ├── Program.cs
│       └── {StarterProjectName}.csproj
└── {SolutionName}.slnx
```

### 结构边界

- 插件项目负责 `PluginBase` 派生类、业务代码、`plugin.yaml`
- starter 项目负责 `Program.cs`、启动参数、运行配置入口、宿主编排
- starter 项目通过 `ProjectReference` 引用插件项目
- `PluginWebAppDefaults.RunAsync<TPlugin>()` 与 `YggdrasilHost.CreateBuilder(...)` 默认位于 starter

## 目录职责清单

### 插件主体项目

- `Config/`
  放配置类与配置相关辅助代码
- `Config/PluginConfigs/`
  放插件自身配置类
- `Config/{ThirdPartyName}/`
  放第三方配置类
- `wwwroot/`
  放静态资源
- `Controllers/`
  放 API 控制器
- `Mapper/`
  放模型映射器
- `Models/VO/`
  放展示用输出模型
- `Models/DTO/`
  放输入输出传输模型
- `Models/Entities/`
  放数据库实体
- `Domains/IRepositories/`
  放仓储接口
- `Domains/Repositories/`
  放仓储实现
- `Services/IServices/`
  放服务接口
- `Services/Services/`
  放服务实现
- `Extensions/`
  放扩展方法
- `Middlewares/`
  放中间件

### starter / 宿主项目

- `Program.cs`
  放启动入口
- `GlobalUsings.cs`
  放 starter 自己的全局 using
- `app.yaml`
  放主运行配置
- `{StarterProjectName}.csproj`
  放启动依赖与对插件项目的 `ProjectReference`

## 固定边界

- `Program.cs` 默认属于 starter / host 项目
- `plugin.yaml` 默认属于插件主体项目
- `app.yaml` 默认由 starter / host 项目加载
- `Config/` 不存放 YAML 根文件
- `Mapper/` 不承载业务逻辑
- `Controllers/` 不直接访问数据库
- `Services/` 不直接承担展示模型输出职责
