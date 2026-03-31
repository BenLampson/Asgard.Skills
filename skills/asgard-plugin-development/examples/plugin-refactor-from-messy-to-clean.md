# 从混乱插件到清晰结构

这个示例不复刻具体业务代码，只抽象出常见混乱形态与重构方向。

## 常见混乱写法

```text
PluginProject/
├── Program.cs                 # 写满宿主构建逻辑
├── plugin.yaml                # 同时混入宿主级和插件级配置
└── Bootstrap/
    ├── MyPlugin.cs            # 超大入口类，负责所有事情
    ├── UserController.cs
    ├── UserService.cs
    ├── UserRepository.cs
    └── SeedData.cs
```

## 问题表现

- `Program.cs` 不再是极薄入口，失去插件项目最短闭环
- `MyPlugin.cs` 同时承担配置绑定、服务注册、启动初始化、种子数据、日志与协调逻辑
- 业务代码回流到 `Bootstrap/`
- 注册阶段如果开始 `BuildServiceProvider()`，会直接破坏生命周期边界
- `plugin.yaml` 边界模糊，后续难以判断配置归属

## 推荐重构目标

```text
PluginProject/
├── Program.cs
├── plugin.yaml
├── Bootstrap/
│   ├── MyPlugin.cs
│   ├── MyFeatureModuleRegistrationExtensions.cs
│   └── Configuration/
│       └── MyPluginConfig.cs
├── MyFeature/
│   ├── Api/
│   ├── Application/
│   ├── Domain/
│   └── Infrastructure/
└── Jobs/
    └── SeedDataJob.cs
```

## 重构步骤

1. 先把 `Program.cs` 收回到一行入口
2. 把插件入口类里的批量服务注册迁移到模块扩展类
3. 把配置对象移到 `Bootstrap/Configuration/`
4. 把业务实现迁移到功能目录
5. 把启动期重逻辑改为轻量协调，或转为作业/应用服务

## 判断是否收敛成功

- 看插件入口类时，能在几分钟内理解“这个插件做什么、依赖什么、在哪注册”
- 看 `Bootstrap/` 时，不会误以为它是业务主目录
- 看 `plugin.yaml` 时，能明确哪些配置只属于当前插件
- 看生命周期代码时，不会出现注册阶段偷跑解析服务的写法
