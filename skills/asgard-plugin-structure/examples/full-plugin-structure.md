# 完整 Asgard 项目结构示例

## 模式 B：插件项目 + starter 项目分离

```text
UserCenter/
├── src/
│   ├── UserCenterPlugin/
│   │   ├── plugin.yaml
│   │   ├── GlobalUsings.cs
│   │   ├── UserCenterPlugin.csproj
│   │   ├── UserCenterPlugin.cs
│   │   ├── Config/
│   │   │   ├── PluginConfigs/
│   │   │   │   └── UserCenterPluginConfig.cs
│   │   │   └── WeChat/
│   │   │       └── WeChatApiConfig.cs
│   │   ├── wwwroot/
│   │   │   └── assets/
│   │   │       └── logo.png
│   │   ├── Controllers/
│   │   │   └── UserController.cs
│   │   ├── Mapper/
│   │   │   └── UserMapper.cs
│   │   ├── Models/
│   │   │   ├── VO/
│   │   │   │   └── UserVo.cs
│   │   │   ├── DTO/
│   │   │   │   ├── CreateUserDto.cs
│   │   │   │   ├── UpdateUserDto.cs
│   │   │   │   └── QueryUserDto.cs
│   │   │   └── Entities/
│   │   │       └── UserEntity.cs
│   │   ├── Domains/
│   │   │   ├── IRepositories/
│   │   │   │   └── IUserRepository.cs
│   │   │   └── Repositories/
│   │   │       └── UserRepository.cs
│   │   ├── Services/
│   │   │   ├── IServices/
│   │   │   │   └── IUserService.cs
│   │   │   └── Services/
│   │   │       └── UserService.cs
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs
│   │   ├── Middlewares/
│   │   │   └── TenantMiddleware.cs
│   │   └── Integrations/
│   │       └── UserSyncClient.cs
│   └── UserCenterPlugin.Starter/
│       ├── app.yaml
│       ├── GlobalUsings.cs
│       ├── Program.cs
│       └── UserCenterPlugin.Starter.csproj
└── UserCenter.slnx
```

## 固定流转

```text
数据库 -> UserEntity -> UserService(转 DTO) -> UserController(转 VO) -> 前端
```

## 关键点

- `UserCenterPlugin` 负责插件主体实现，不承载默认启动入口
- `UserCenterPlugin.Starter` 负责 `Program.cs`、`app.yaml`、调试与启动
- starter 通过 `ProjectReference` 引用 `UserCenterPlugin`
- `UserController` 只暴露 API，不直接接触数据库
- `UserService` 处理业务并组织 DTO，但访问数据库时仍然通过 `IUserRepository` / `UserRepository`
- `UserMapper` 统一做模型映射，优先 attribute
- `plugin.yaml` 位于插件主体项目；`app.yaml` 由 starter 加载
