---
name: asgard-plugin-structure
description: Asgard 插件项目结构 skill。Use when scaffolding a new Asgard plugin project, defining folder layout, choosing starter files, placing DTO/VO/Entity/Repository/Service code, configuring GlobalUsings.cs, or deciding where Mapper, Config, app.yaml, and plugin.yaml belong.
---

# Asgard Plugin Structure

## 作用

这个 skill 是 **Asgard 插件项目结构的唯一权威来源**。

它只负责定义：

- 项目目录长什么样
- 基础文件放哪里
- 依赖怎么装
- `GlobalUsings.cs` 怎么放
- `DTO / VO / Entity / Repository / Service / Mapper` 应该放在哪
- 层间数据怎么流转

它**不负责**讲某一层的具体实现细节。

具体写法请继续引用：

- 控制器：`$asgard-api-development`
- 数据库与实体：`$asgard-database`
- 仓储与服务注册：`$asgard-repository-service-registration`
- 配置绑定：`$asgard-configuration`
- 生命周期：`$asgard-plugin-lifecycle`
- 强制编码规则：`$asgard-dotnet-10-csharp-14`

## 什么时候使用

在以下场景必须优先使用本 skill：

- 新建 Asgard 插件项目
- 搭项目骨架
- 设计目录结构
- 决定 `GlobalUsings.cs`、`Program.cs`、`app.yaml`、`plugin.yaml` 放置位置
- 决定 `Mapper`、`Models`、`Domains`、`Services` 的归属
- 决定 `DTO / VO / Entity` 的分层与流转

## 标准依赖

新插件项目默认只安装以下依赖：

```xml
<ItemGroup>
  <PackageReference Include="Asgard.Analyzers" />
  <PackageReference Include="Asgard.PluginSdk" />
</ItemGroup>
```

## 标准基础文件

这些文件默认属于 starter 的硬组成部分：

- `GlobalUsings.cs`
- `Program.cs`
- `app.yaml`
- `plugin.yaml`

## 标准目录树

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

## 固定职责

- `Config/`
  放配置类、配置绑定辅助类型、第三方集成配置相关代码
- `Config/PluginConfigs/`
  放 Asgard 插件自身配置类
- `Config/{ThirdPartyName}/`
  放第三方组件或外部系统配置类
- `wwwroot/`
  放静态资源
- `Controllers/`
  放所有 API 声明
- `Mapper/`
  放对象映射器
- `Models/VO/`
  放对外展示模型
- `Models/DTO/`
  放数据传输模型
- `Models/Entities/`
  放数据库实体模型
- `Domains/IRepositories/`
  放仓储接口
- `Domains/Repositories/`
  放仓储实现
- `Services/IServices/`
  放服务接口
- `Services/Services/`
  放服务实现
- `Extensions/`
  放扩展方法和扩展装配
- `Middlewares/`
  放自定义中间件
- `yyy/`
  放不适合归到上述目录但仍属于插件内部的自定义内容

`app.yaml` 与 `plugin.yaml` 始终位于项目根目录。`Config/` 不承担 YAML 根文件职责。

## 固定流转

输出链路固定为：

```text
数据库
   ↓
Entity  【查询】
   ↓
Service → 转 DTO  【业务处理】
   ↓
Controller → 转 VO 【给前端展示】
   ↓
前端页面
```

进入链路按反方向理解：

```text
前端请求
   ↓
Controller
   ↓
Service
   ↓
Repository
   ↓
Entity
   ↓
数据库
```

## Mapper 规则

- `Mapper/` 默认一类一映射器
- 优先使用 attribute 标注映射关系
- 只有 attribute 无法表达时，才在 `Mapper/` 中补显式配置
- 不要把转换逻辑散落在 Controller 或 Service 里
- `Asgard.PluginSdk` 已经带上常用映射能力，默认先用框架已有能力，不要另起一套

## 使用原则

- 结构问题只在本 skill 定义
- 其他 skill 可以提示“某类文件通常位于哪个目录”
- 其他 skill 不允许再定义第二套完整目录树
- 代码实现仍必须遵守 `$asgard-dotnet-10-csharp-14`

## 参考资源

- `references/project-layout.md`
- `references/layer-flow.md`
- `references/package-globalusings-and-mapper.md`
- `templates/AsgardPlugin.csproj.template`
- `templates/GlobalUsings.cs.template`
- `templates/Program.cs.template`
- `templates/app.yaml.template`
- `templates/plugin.yaml.template`
- `templates/project-tree.txt.template`
- `examples/minimal-plugin-structure.md`
- `examples/full-plugin-structure.md`
