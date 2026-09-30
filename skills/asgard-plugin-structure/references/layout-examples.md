## 标准目录树

### 多业务模块分组规则

当插件主体包含多个业务模块、聚合或子域时，默认采用：

```text
第一层：Asgard 标准层
第二层：业务模块 / 聚合 / 子域
```

也就是优先保持 `Controllers/`、`Mapper/`、`Models/`、`Domains/`、`Services/` 等顶层标准层清晰，再在这些目录内部按业务模块继续分组。

推荐：

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

不推荐把业务模块作为第一层，再在模块内部重复一套标准层：

```text
{PluginProjectName}/
└── {BusinessModuleName}/
    ├── Models/
    ├── Domains/
    └── Services/
```

例外：业务模块中存在独立的领域引擎、DSL、规则定义、协议适配或运行时内核时，可以保留一个顶层 `{BusinessModuleName}/` 目录承载这些非 CRUD 分层代码；但该模块的 Controller、DTO、VO、Entity、Repository、Service 默认仍归入标准层目录下的模块子目录。

### 模式 A：单项目快速验证

```text
{ProjectName}/
├── app.yaml
├── plugin.yaml
├── GlobalUsings.cs
├── Program.cs
├── {ProjectName}.csproj
├── Config/
│   ├── PluginConfigs/
│   │   └── {PluginConfigClassName}.cs
│   └── {ThirdPartyName}/
│       └── {ThirdPartyConfigClassName}.cs
├── wwwroot/
│   └── {StaticAssetFiles}
├── Controllers/
│   └── {FeatureName}Controller.cs
├── Mapper/
│   └── {AggregateName}Mapper.cs
├── Models/
│   ├── VO/
│   │   └── {AggregateName}Vo.cs
│   ├── DTO/
│   │   └── {AggregateName}Dto.cs
│   └── Entities/
│       └── {AggregateName}Entity.cs
├── Domains/
│   ├── IRepositories/
│   │   └── I{AggregateName}Repository.cs
│   └── Repositories/
│       └── {AggregateName}Repository.cs
├── Services/
│   ├── IServices/
│   │   └── I{AggregateName}Service.cs
│   └── Services/
│       └── {AggregateName}Service.cs
├── Extensions/
│   └── {FeatureName}Extensions.cs
├── Middlewares/
│   └── {FeatureName}Middleware.cs
└── {CustomModuleName}/
    └── {CustomFiles}
```

### 模式 B：插件项目 + starter 项目分离

```text
{SolutionRoot}/
├── src/
│   ├── {PluginProjectName}/
│   │   ├── plugin.yaml
│   │   ├── GlobalUsings.cs
│   │   ├── {PluginProjectName}.csproj
│   │   ├── {PluginClassName}.cs
│   │   ├── Config/
│   │   │   ├── PluginConfigs/
│   │   │   │   └── {PluginConfigClassName}.cs
│   │   │   └── {ThirdPartyName}/
│   │   │       └── {ThirdPartyConfigClassName}.cs
│   │   ├── wwwroot/
│   │   │   └── {StaticAssetFiles}
│   │   ├── Controllers/
│   │   │   └── {FeatureName}Controller.cs
│   │   ├── Mapper/
│   │   │   └── {AggregateName}Mapper.cs
│   │   ├── Models/
│   │   │   ├── VO/
│   │   │   │   └── {AggregateName}Vo.cs
│   │   │   ├── DTO/
│   │   │   │   └── {AggregateName}Dto.cs
│   │   │   └── Entities/
│   │   │       └── {AggregateName}Entity.cs
│   │   ├── Domains/
│   │   │   ├── IRepositories/
│   │   │   │   └── I{AggregateName}Repository.cs
│   │   │   └── Repositories/
│   │   │       └── {AggregateName}Repository.cs
│   │   ├── Services/
│   │   │   ├── IServices/
│   │   │   │   └── I{AggregateName}Service.cs
│   │   │   └── Services/
│   │   │       └── {AggregateName}Service.cs
│   │   ├── Extensions/
│   │   │   └── {FeatureName}Extensions.cs
│   │   ├── Middlewares/
│   │   │   └── {FeatureName}Middleware.cs
│   │   └── {CustomModuleName}/
│   │       └── {CustomFiles}
│   └── {StarterProjectName}/
│       ├── app.yaml
│       ├── GlobalUsings.cs
│       ├── Program.cs
│       └── {StarterProjectName}.csproj
└── {SolutionName}.slnx
```
