# Asgard 项目结构说明

## 推荐结论

- 正式开发优先采用“插件主体项目 + starter 项目分离”
- 单项目结构只作为快速验证模式存在
- 不要默认把 `Program.cs`、`app.yaml`、`plugin.yaml` 都说成插件项目根目录标配
- 多业务模块插件默认“顶层按 Asgard 标准层，层内按业务模块分组”

## 多业务模块分组规则

正式项目中如果存在多个业务模块、聚合或子域，默认不要把业务模块作为第一层目录后再重复一套 `Models / Domains / Services`。推荐先固定 Asgard 标准层，再在层内按业务模块分组。

推荐结构：

```text
{PluginProjectName}/
├── Controllers/
│   ├── {BusinessModuleName}/
│   └── {OtherModuleName}/
├── Mapper/
│   ├── {BusinessModuleName}/
│   └── {OtherModuleName}/
├── Models/
│   ├── DTO/
│   │   ├── {BusinessModuleName}/
│   │   └── {OtherModuleName}/
│   ├── VO/
│   │   ├── {BusinessModuleName}/
│   │   └── {OtherModuleName}/
│   └── Entities/
│       ├── {BusinessModuleName}/
│       └── {OtherModuleName}/
├── Domains/
│   ├── IRepositories/
│   │   ├── {BusinessModuleName}/
│   │   └── {OtherModuleName}/
│   └── Repositories/
│       ├── {BusinessModuleName}/
│       └── {OtherModuleName}/
└── Services/
    ├── IServices/
    │   ├── {BusinessModuleName}/
    │   └── {OtherModuleName}/
    └── Services/
        ├── {BusinessModuleName}/
        └── {OtherModuleName}/
```

不推荐结构：

```text
{PluginProjectName}/
└── {BusinessModuleName}/
    ├── Models/
    ├── Domains/
    └── Services/
```

例外：某个业务模块拥有独立领域引擎、DSL、规则定义、协议适配或运行时内核时，可以保留顶层 `{BusinessModuleName}/` 放这些非 CRUD 分层代码。该模块的 Controller、DTO、VO、Entity、Repository、Service 默认仍放回标准层目录下的模块子目录。

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
- 新建正式插件骨架时，默认只需要插件主体项目和 starter 项目
- 不要因为业务名里有 Web、Agent、Task、Script、Hub 等概念，就自动创建 `.Web`、`.Agent`、`.Worker`、`.Api` 项目
- 只有明确存在独立运行、独立部署或独立生命周期时，才拆额外项目

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
  放模型映射器；多业务模块时可继续分 `{BusinessModuleName}/`
- `Models/VO/`
  放展示用输出模型；多业务模块时可继续分 `{BusinessModuleName}/`
- `Models/DTO/`
  放输入输出传输模型；多业务模块时可继续分 `{BusinessModuleName}/`
- `Models/Entities/`
  放数据库实体；多业务模块时可继续分 `{BusinessModuleName}/`
- `Domains/IRepositories/`
  放仓储接口；多业务模块时可继续分 `{BusinessModuleName}/`
- `Domains/Repositories/`
  放仓储实现；多业务模块时可继续分 `{BusinessModuleName}/`
- `Services/IServices/`
  放服务接口；多业务模块时可继续分 `{BusinessModuleName}/`
- `Services/Services/`
  放服务实现；多业务模块时可继续分 `{BusinessModuleName}/`
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
- API Controller、Hub、后台任务、脚本执行服务、Agent 相关实体默认仍放插件主体项目
- `.Web` 仅表示独立 Web 宿主或前端承载层，不表示“有 Controller”
- `.Agent` 仅表示独立 agent runtime 或远程执行进程，不表示“有 AgentEntity”
