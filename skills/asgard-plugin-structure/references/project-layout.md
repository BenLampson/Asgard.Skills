# Asgard 插件项目标准目录

## 完整目录树

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
└── yyy/
    └── {CustomFiles}
```

## 目录职责清单

- 根目录
  放项目入口和 YAML 根文件
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
- `yyy/`
  放不属于以上目录的自定义代码

## 固定边界

- `app.yaml` 和 `plugin.yaml` 始终位于项目根目录
- `Config/` 不存放 YAML 根文件
- `Mapper/` 不承载业务逻辑
- `Controllers/` 不直接访问数据库
- `Services/` 不直接承担展示模型输出职责
