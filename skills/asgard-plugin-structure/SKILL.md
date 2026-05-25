---
name: asgard-plugin-structure
description: Asgard 插件项目结构 skill。Use when scaffolding a new Asgard plugin project, defining folder layout, choosing starter files, separating plugin implementation from starter/host bootstrap, placing DTO/VO/Entity/Repository/Service code, configuring GlobalUsings.cs, or deciding where Mapper, Config, app.yaml, and plugin.yaml belong.
---

# Asgard Plugin Structure

## 作用

这个 skill 是 **Asgard 插件项目结构的权威来源**。

它负责定义：

- 插件主体项目怎么组织
- starter / 宿主启动项目怎么组织
- `Program.cs`、`plugin.yaml`、`app.yaml` 各自默认归属
- `GlobalUsings.cs`、`ProjectReference`、基础依赖怎么放
- `DTO / VO / Entity / Repository / Service / Mapper` 应该放在哪
- 插件主体与启动承载方的职责边界

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
- 设计插件项目与 starter 项目的分工
- 决定 `GlobalUsings.cs`、`Program.cs`、`app.yaml`、`plugin.yaml` 放置位置
- 决定 `Mapper`、`Models`、`Domains`、`Services` 的归属
- 决定 `DTO / VO / Entity` 的分层与流转
- 判断当前仓库属于单项目快速验证，还是双项目分离

## 推荐项目组织方式

Asgard 当前认可、也更希望推广的形式是：

- `Asgard.Heimdall` 这类项目负责插件主体实现
- `Asgard.Heimdall.Starter` 这类项目负责启动入口与调试承载
- 正式开发、长期维护、业务复杂度上来后，**优先推荐插件项目 + starter 项目分离**
- 新建插件骨架时，默认只创建插件主体项目和 starter 项目；不要凭业务名自动追加 `.Agent`、`.Web`、`.Api`、`.Worker` 等项目
- 只有存在明确独立运行边界时才新增额外项目，例如独立进程 Agent runtime、独立前端/Web 宿主、独立 worker 部署单元

可以接受两种模式：

### 模式 A：单项目快速验证

适用场景：

- PoC
- Demo
- 临时验证插件生命周期或配置装配
- 业务代码很少，且短期内不会继续扩展

特点：

- `Program.cs`、`plugin.yaml`、业务代码位于同一项目
- `app.yaml` 可以直接放在同项目根目录
- 适合最短路径验证，但**不是默认推荐的长期结构**

### 模式 B：插件项目 + starter 项目分离

适用场景：

- 正式开发
- 长期维护
- 需要独立调试、发布、复用插件主体
- 需要清晰区分“插件实现”与“启动承载”

特点：

- 插件主体项目只承载插件实现、业务目录、插件清单
- starter 项目承载 `Program.cs`、启动参数、宿主编排、调试入口
- starter 项目通过 `ProjectReference` 引用插件主体项目
- `PluginWebAppDefaults.RunAsync<TPlugin>()` 通常位于 starter，而不是业务插件主体项目
- API Controller、DTO、Entity、Repository、Service 默认都属于插件主体项目，不需要再拆一个 `.Web` 项目
- 后台任务、脚本任务、Job Handler 默认也属于插件主体项目；只有需要独立部署、独立长连接或独立进程生命周期时，才拆 `.Agent` / `.Worker`

### 额外项目判定规则

默认不要创建这些项目：

- `{PluginName}.Web`
  除非它是独立 Web 宿主、独立前端承载层，或必须与插件主体分开部署
- `{PluginName}.Agent`
  除非它是独立进程、远程执行器、长连接 agent runtime，或有独立发布生命周期
- `{PluginName}.Api`
  除非它不是插件 Controller，而是独立 API 应用

如果只是“插件里有 Web API、Hub、后台任务、脚本执行、Agent 实体模型”，默认仍放在插件主体项目对应目录中。

## 标准依赖

### 插件主体项目默认依赖

```xml
<ItemGroup>
  <PackageReference Include="Asgard.Analyzers" />
  <PackageReference Include="Asgard.PluginSdk" />
</ItemGroup>
```

### starter 项目默认职责

- 引用插件主体项目
- 提供启动入口
- 决定 `app.yaml` 的加载路径
- 根据需要补充宿主级依赖与调试配置
- `AsgardStarter.csproj` 使用 `Microsoft.NET.Sdk`，**不是必须**改成 `Microsoft.NET.Sdk.Web`。  
  对于只承载启动与配置的 starter，框架启动链路本身不依赖 Web SDK；只有当你明确需要 Web SDK 默认行为时，才改用 `Web`。

### starter 项目可选 SDK 说明

- `Microsoft.NET.Sdk`：推荐默认。更轻量，启动与 `host.*` 配置都可正常生效。
- `Microsoft.NET.Sdk.Web`：仅在你确认需要 Web SDK 提供的默认行为时才切换。
- 使用者不要把“starter 必须是 Web SDK”当规则；这是一个常见误解，需要按项目目标来选。

## 基础文件归属

- `Program.cs`
  默认属于 starter / host / 启动项目，不要默认放进插件主体项目
- `plugin.yaml`
  默认属于插件主体项目，是插件清单与插件级元数据
- `app.yaml`
  属于运行配置入口，是否与插件项目同目录取决于组织方式
- `GlobalUsings.cs`
  插件项目与 starter 项目都可以有，但各自只维护自己的全局 using

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

## 固定职责

### 插件主体项目职责

- `{PluginClassName}.cs`
  插件入口类，继承 `PluginBase`
- `Config/`
  放配置类、配置绑定辅助类型、第三方集成配置相关代码
- `Config/PluginConfigs/`
  放 Asgard 插件自身配置类
- `Config/{ThirdPartyName}/`
  放第三方组件或外部系统配置类
- `wwwroot/`
  放静态资源
- `Controllers/`
  放所有 API 声明；Controller 只负责输入输出编排，把 Service 返回的 DTO 转成 VO 后，再统一包装成 `Response<T>` / `PageResponse<T>` / `CursorResponse<T>` 对外返回
- `Mapper/`
  放对象映射器；多业务模块时可继续分 `{BusinessModuleName}/`
- `Models/VO/`
  放对外展示模型；多业务模块时可继续分 `{BusinessModuleName}/`
- `Models/DTO/`
  放数据传输模型；多业务模块时可继续分 `{BusinessModuleName}/`
- `Models/Entities/`
  放数据库实体模型；多业务模块时可继续分 `{BusinessModuleName}/`
- `Domains/IRepositories/`
  放仓储接口；多业务模块时可继续分 `{BusinessModuleName}/`
- `Domains/Repositories/`
  放仓储实现；多业务模块时可继续分 `{BusinessModuleName}/`
- `Services/IServices/`
  放服务接口；多业务模块时可继续分 `{BusinessModuleName}/`
- `Services/Services/`
  放服务实现；多业务模块时可继续分 `{BusinessModuleName}/`
- `Extensions/`
  放扩展方法和扩展装配
- `Middlewares/`
  放自定义中间件
- `{CustomModuleName}/`
  这是“其他自定义目录”的占位写法，不是必须创建名为 `yyy` 的真实目录
- `plugin.yaml`
  放插件清单与插件元数据

### starter / 宿主启动项目职责

- `Program.cs`
  启动入口、调试入口、参数解析入口
- `GlobalUsings.cs`
  只维护启动项目自身需要的全局 using
- `ProjectReference`
  引用插件主体项目
- `app.yaml`
  运行配置入口，决定宿主启动期加载的主配置
- `PluginWebAppDefaults.RunAsync<TPlugin>()`
  快速验证或单插件启动的默认位置
- `YggdrasilHost.CreateBuilder(...)`
  宿主构建、插件装配、中间件编排、启动路径选择

## 文件归属补充规则

- 不要再把 `Program.cs`、`app.yaml`、`plugin.yaml` 一刀切地说成“插件项目根目录标配”
- `Program.cs` 默认属于 starter；只有在模式 A 快速验证时，才与插件实现同项目
- `plugin.yaml` 默认属于插件主体项目
- `app.yaml` 默认由启动承载方加载
- 在模式 B 中，如果插件项目需要携带运行配置资源，可以包含 `app.yaml` 作为输出资源，但必须明确说明由 starter / host 加载，或复制到运行目录后再加载
- 多业务模块项目默认顶层按 Asgard 标准层组织，层内再按业务模块分子目录
- 不要默认把 `SupplyChain/Models`、`SupplyChain/Services`、`SupplyChain/Domains` 这类业务模块优先结构作为正式推荐结构
- 不要因为存在 `Hub`、`Task`、`Script`、`AgentEntity` 这类类型名就推导出需要 `.Web` 或 `.Agent` 独立项目

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

实际代码访问时，`Service` 仍然必须通过 `Repository` 访问 `Entity` 与数据库。上面的输出链路强调的是模型转换职责，不表示可以跳过仓储层。

**补充强制要求：**

- `Controller` 层对外输出时，必须把结果包装成 Asgard 统一响应模型
- `Service` 层内部返回仍然遵循 DTO 边界，不直接承担统一响应壳职责
- 非分页接口使用 `Response<T>` / `Response<object>`
- 页码分页接口使用 `PageResponse<T>`
- 游标分页接口使用 `CursorResponse<T>`
- 不允许在 `Controllers/` 中直接返回裸 `VO`、裸集合或自定义另一套响应壳

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
- 优先使用 `Asgard.PluginSdk` 提供的 `PluginObjectMapper`
- `PluginObjectMapper` 默认支持 `class -> class` 同名同类型（或类型兼容）属性映射，不要求固定继承基类
- 简单对象转换优先使用 `PluginObjectMapper.Map<TSource, TDestination>(source)`
- 更新既有实体优先使用 `PluginObjectMapper.MapToExisting<TSource, TDestination>(source, destination)`
- 列表转换优先使用 `PluginObjectMapper.MapList<TSource, TDestination>(source)`

## 回答结构问题时的默认判断顺序

1. 先判断用户当前仓库是单项目模式，还是双项目分离
2. 如果仓库已经采用“插件实现 + starter 启动器”分离结构，优先尊重现有结构
3. 如果用户只是做快速验证，可以提供模式 A
4. 如果用户在做正式开发或维护现有业务，优先推荐模式 B
5. 额外项目必须先证明独立运行、独立部署或独立生命周期，不要按名称臆造 `.Agent` / `.Web`
6. 不要把“快速验证示例”表述成唯一标准结构

## 使用原则

- 结构问题只在本 skill 定义
- 其他 skill 可以提示“某类文件通常位于哪个目录”
- 其他 skill 不允许再定义第二套相互冲突的完整目录树
- 代码实现仍必须遵守 `$asgard-dotnet-10-csharp-14`

## 参考资源

- `references/project-layout.md`
- `references/layer-flow.md`
- `references/package-globalusings-and-mapper.md`
- `templates/AsgardPlugin.csproj.template`
- `templates/AsgardStarter.csproj.template`
- `templates/GlobalUsings.cs.template`
- `templates/Program.cs.template`
- `templates/app.yaml.template`
- `templates/plugin.yaml.template`
- `templates/project-tree.txt.template`
- `examples/minimal-plugin-structure.md`
- `examples/full-plugin-structure.md`
