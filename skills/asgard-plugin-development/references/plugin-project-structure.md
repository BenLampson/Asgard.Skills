# 内建插件项目结构

## 推荐最小结构

```text
{PluginProject}/
├── Program.cs
├── plugin.yaml
├── GlobalUsings.cs
├── Bootstrap/
│   ├── {PluginClassName}.cs
│   ├── {FeatureName}ModuleRegistrationExtensions.cs
│   └── Configuration/
│       └── {PluginConfigClassName}.cs
├── {FeatureName}/
│   ├── Api/
│   ├── Application/
│   ├── Domain/
│   └── Infrastructure/
└── Jobs/
    └── {JobClassName}.cs
```

## 目录职责

- `Program.cs`
  只保留插件项目入口，默认是一行 `PluginWebAppDefaults.RunAsync<TPlugin>()`
- `plugin.yaml`
  只放插件配置和插件作业，不放宿主级配置
- `Bootstrap/`
  只放插件入口、模块装配、配置对象等“启动拼装层”代码
- `{FeatureName}/`
  放业务能力代码，按功能或层级拆分
- `Jobs/`
  放自动加载或手动调度的作业实现

## 强制组织建议

- `Bootstrap/` 不要承载业务实现
- 插件入口类不要膨胀成“大总管”
- 新增业务能力时，优先新增功能目录，而不是继续扩写插件入口
- `Program.cs` 保持极薄，避免把宿主编排逻辑搬进插件项目

## 从混乱结构提炼出的规则

- 如果插件入口类同时负责配置绑定、模块装配、启动初始化、种子数据、端点映射，说明职责已经过载
- 如果业务控制器、应用服务、仓储都挤在 `Bootstrap/` 附近，说明目录边界没有建立好
- 如果 `plugin.yaml` 里混入了全局系统配置，后续维护会很快失控
- 如果项目一开始就没有 `ModuleRegistrationExtensions` 这类装配出口，服务注册通常会回流到插件入口类
