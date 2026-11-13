namespace Asgard.Yggdrasil.AspNetCore;

public partial class YggdrasilHostBuilder
{
    /// <summary>注册由宿主配置驱动的 OpenAPI 文档。</summary>
    private void RegisterOpenApi(IServiceCollection services)
    {
        if (_hostConfig.OpenApi is not { Enabled: true } config)
        {
            return;
        }

        var bearerEnabled = _hostConfig.Auth is { Enabled: true };
        _ = services.AddOpenApi(config.Version, options =>
        {
            // 选用 3.1，避免运行时升级导致文档规范版本随默认值变化。
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            _ = options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = config.Title;
                document.Info.Version = config.Version;
                document.Info.Description = config.Description;

                if (bearerEnabled)
                {
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                    document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "输入 JWT，Scalar 会自动添加 Bearer 前缀。"
                    };
                }

                return Task.CompletedTask;
            });
            _ = options.AddOperationTransformer(new ControllerXmlCommentsTransformer());

            if (bearerEnabled)
            {
                _ = options.AddOperationTransformer(DescribeBearerRequirement);
            }
        });
    }

    /// <summary>仅对声明了授权且未允许匿名的操作标记 Bearer 需求。</summary>
    private static Task DescribeBearerRequirement(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = []
        });
        return Task.CompletedTask;
    }

    /// <summary>映射 OpenAPI JSON 和 Scalar 页面；没有显式启用时不公开端点。</summary>
    private void MapOpenApiDocumentation(WebApplication app)
    {
        if (_hostConfig.OpenApi is not { Enabled: true } config)
        {
            return;
        }

        var route = "/" + config.RoutePrefix.Trim('/');
        _ = app.MapOpenApi();
        _ = app.MapScalarApiReference(route, options =>
        {
            // Scalar 根据页面路径计算应用前缀，文档地址无需重复拼接 PathBase。
            _ = options.WithTitle(config.Title)
                .WithOpenApiRoutePattern("/openapi/{documentName}.json")
                .AddDocument(config.Version);
        });
    }
}
