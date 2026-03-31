# 依赖、GlobalUsings 与 Mapper 规则

## 默认依赖

插件项目默认只安装这两个包：

```xml
<ItemGroup>
  <PackageReference Include="Asgard.Analyzers" />
  <PackageReference Include="Asgard.PluginSdk" />
</ItemGroup>
```

## 默认 GlobalUsings

`GlobalUsings.cs` 是 starter 的固定组成部分。

建议至少包含：

- `System`
- `System.Collections.Generic`
- `System.Threading`
- `System.Threading.Tasks`
- `Asgard.Abstractions`
- `Asgard.Abstractions.AspNetCore.Controller`
- `Asgard.Abstractions.Plugin`
- `Asgard.Core.Plugin`
- `Asgard.PluginSdk`
- `FreeSql`
- `FreeSql.DataAnnotations`
- `Mapster`
- `Microsoft.AspNetCore.Mvc`
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Options`

## Mapper 规则

- `Mapper/` 默认一类一文件
- 优先使用 attribute 标注映射
- 只有 attribute 无法满足时，才写显式集中配置
- `Mapper` 只负责模型转换，不负责业务判断
- `Asgard.PluginSdk` 已经带上常用映射依赖，优先复用，不要额外再造一套映射方案

## 推荐分工

- `Entity -> DTO`
  由 Service 协调并使用 Mapper 完成
- `DTO -> VO`
  由 Controller 层使用 Mapper 完成
- 不要把 Controller 写成手工字段搬运代码集中地
