## TS Gen 使用约定

TsGen 是可选开发工具，不是 Asgard 前端的强制依赖。项目决定采用生成式 TypeScript 客户端时，优先使用官方 `Asgard.TsGen`；项目也可以明确选择 OpenAPI、共享手写客户端或其他契约方案。

### 生成前提

- 只有继承 `ControllerBase` 且显式标记 `[AsgardTsGen]` 的控制器才会被扫描和生成
- 未选择 TsGen 的项目不需要添加 `[AsgardTsGen]`，也不需要保留生成目录
- 未标记该特性的控制器不会进入生成结果
- 控制器返回值仍应遵循 Asgard 统一包装约定，例如 `Response<T>`、`PageResponse<T>`、`CursorResponse<T>` 或 SSE
- 在 Yggdrasil 宿主内通过 `/asgard-tsgen` 导出时，只会导出**当前宿主已经加载的插件程序集**中、且已被 MVC 真实发现到的控制器
- 宿主不会导出未加载插件、宿主自身控制器，或虽在程序集里但未进入 MVC ApplicationPart 的控制器

### 典型用法

```powershell
dotnet run --project Common/Asgard.TsGen/Asgard.TsGen.csproj -- --assembly ./Host/Asgard.Yggdrasil.AspNetCore/bin/Debug/net10.0/Asgard.Yggdrasil.AspNetCore.dll
```

也可以在安装为工具后执行：

```powershell
asgard-tsgen --assembly ./bin/Debug/net10.0/MyApi.dll
```

开发环境下，需要先显式启用 Yggdrasil 宿主导出端点：

```yaml
host:
  tsGen:
    enabled: true
```

然后访问：

```text
http://127.0.0.1:5000/asgard-tsgen
```

实际端口以宿主启动日志中的告警输出为准。宿主会在启动后打印完整访问地址，并在收到导出请求时输出当前插件程序集、MVC 已发现控制器以及最终命中的 TS 导出控制器，便于排查“只生成 common、不生成 controller/models”的问题。

### 输出规则

- 默认输出目录就是执行命令时所在的当前目录
- 生成器会重建自己负责的产物目录，当前固定为 `common/`、`controller/`、`models/`
- 这些目录应视为纯生成目录，不要手写或混入自定义代码
- 如果需要隔离生成结果，请先进入专门的前端客户端目录，再执行生成命令

### 团队约定

- 先由项目明确选择是否使用 TsGen；不要仅因这是 Asgard 项目就强制引入
- 项目选择 TsGen 后，想让某个 API 进入生成结果时再添加 `[AsgardTsGen]`
- 使用 TsGen 的项目修改控制器路由、参数或返回模型后，应重新生成
- 使用 TsGen 的前端应以最新生成结果为准，不要继续引用已删除的旧接口文件
- 如果宿主导出结果只出现 `common/`，优先检查：插件是否真的已加载、控制器是否被 MVC 发现、控制器是否显式标记 `[AsgardTsGen]`
