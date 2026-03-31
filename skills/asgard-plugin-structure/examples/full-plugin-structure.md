# 完整 Asgard 插件结构示例

## 目录树

```text
UserCenterPlugin/
├── app.yaml
├── plugin.yaml
├── GlobalUsings.cs
├── Program.cs
├── UserCenterPlugin.csproj
├── Config/
│   ├── PluginConfigs/
│   │   └── UserCenterPluginConfig.cs
│   └── WeChat/
│       └── WeChatApiConfig.cs
├── wwwroot/
│   └── assets/
│       └── logo.png
├── Controllers/
│   └── UserController.cs
├── Mapper/
│   └── UserMapper.cs
├── Models/
│   ├── VO/
│   │   └── UserVo.cs
│   ├── DTO/
│   │   ├── CreateUserDto.cs
│   │   ├── UpdateUserDto.cs
│   │   └── QueryUserDto.cs
│   └── Entities/
│       └── UserEntity.cs
├── Domains/
│   ├── IRepositories/
│   │   └── IUserRepository.cs
│   └── Repositories/
│       └── UserRepository.cs
├── Services/
│   ├── IServices/
│   │   └── IUserService.cs
│   └── Services/
│       └── UserService.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Middlewares/
│   └── TenantMiddleware.cs
└── yyy/
    └── Integrations/
        └── UserSyncClient.cs
```

## 固定流转

```text
数据库 -> UserEntity -> UserService(转 DTO) -> UserController(转 VO) -> 前端
```

## 关键点

- `UserController` 只暴露 API，不直接接触数据库
- `UserService` 处理业务并组织 DTO
- `UserMapper` 统一做模型映射，优先 attribute
- `IUserRepository` 与 `UserRepository` 分别位于 `Domains/IRepositories` 与 `Domains/Repositories`
- `IUserService` 与 `UserService` 分别位于 `Services/IServices` 与 `Services/Services`
