# Asgard 7.0：OpenAPI 与 Scalar

使用 `YggdrasilHost.CreateBuilder(...)` 或 `PluginWebAppDefaults.RunAsync<TPlugin>(...)` 时，在启动方加载的 `app.yaml` 中设置：

~~~yaml
host:
  openApi:
    enabled: true
    title: "Orders API"
    version: "v1"
    description: "订单服务接口"
    routePrefix: "scalar"
~~~

- OpenAPI 3.1 JSON 默认在 `/openapi/v1.json`。 `version` 同时决定文档版本与文件名。
- Scalar 默认在 `/scalar`。 `routePrefix` 只改变 UI 路由；反向代理要放行该前缀下的页面和静态资源，以及 `/openapi/` 下的 JSON。
- 默认关闭；开启后所有运行环境都映射。需要限制访问时，在部署入口对页面和 JSON 一起设置访问规则。
- 启用宿主 `host.auth.enabled` 后，OpenAPI 为声明授权且未允许匿名的操作标注 JWT Bearer。Scalar 中填原始 JWT，页面会添加 `Bearer` 前缀。文档声明不改变服务端授权。
- 为插件控制器输出 XML 注释并随程序集部署；框架会从同目录 XML 补充操作摘要、备注、参数和已有响应的说明。继续用 `[ProducesResponseType]` 声明实际返回的 Asgard 响应模型。
- 使用默认宿主时，不再手工注册 `AddOpenApi()`、`MapOpenApi()` 或 `MapScalarApiReference()`。自定义文档时按 ASP.NET Core 11 的 transformer API 扩展，并校验生成 JSON。

## 升级到 Asgard 7.x

从 Asgard 6.x 或更早版本迁移时：

1. 将 starter 和插件改为 `net11.0`，移除旧的 C# 版本限制或显式设置 `LangVersion=15`。安装 .NET 11 SDK、更新运行时与容器基础镜像，并将 Asgard 包统一升到同一 7.x 版本后重编插件。
2. 将 `host.swagger` 改成 `host.openApi`。旧配置、旧 Swagger UI 和 `/swagger/{version}/swagger.json` 已移除。升级自定义 Swashbuckle 过滤器时改用 OpenAPI transformer。
3. 更新反向代理、文档链接与客户端生成脚本。对比新旧 JSON 的操作、鉴权与 schema，并用 Scalar 发起一次真实 API 请求。
