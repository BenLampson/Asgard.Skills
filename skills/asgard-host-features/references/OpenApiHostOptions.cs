namespace Asgard.Abstractions.AspNetCore.Host;

/// <summary>
/// OpenAPI 文档与 Scalar 交互页面配置。
/// </summary>
public sealed class OpenApiHostOptions
{
    /// <summary>是否公开 OpenAPI 文档和 Scalar 页面。</summary>
    [ConfigPath("host.openApi.enabled", DefaultValue = false)]
    public bool Enabled { get; set; }

    /// <summary>API 文档标题。</summary>
    [ConfigPath("host.openApi.title", DefaultValue = "Asgard API")]
    public string Title { get; set; } = "Asgard API";

    /// <summary>API 文档说明。</summary>
    [ConfigPath("host.openApi.description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>API 文档版本，同时用作文档地址中的名称。</summary>
    [ConfigPath("host.openApi.version", DefaultValue = "v1")]
    public string Version { get; set; } = "v1";

    /// <summary>Scalar 页面路由前缀，默认为 <c>/scalar</c>。</summary>
    [ConfigPath("host.openApi.routePrefix", DefaultValue = "scalar")]
    public string RoutePrefix { get; set; } = "scalar";

    /// <summary>验证已启用的文档配置。</summary>
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("OpenAPI 标题不能为空。");
        }

        if (!IsPathSegment(Version))
        {
            throw new InvalidOperationException("OpenAPI 版本号必须是有效的 URL 路径片段。");
        }

        if (string.IsNullOrWhiteSpace(RoutePrefix) || !RoutePrefix.Trim('/').Split('/').All(IsPathSegment))
        {
            throw new InvalidOperationException("Scalar 路由前缀必须是有效的 URL 路径。");
        }
    }

    /// <summary>只接受可直接用在路由和文档地址中的固定片段。</summary>
    private static bool IsPathSegment(string value) =>
        value is not ("" or "." or "..")
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or '~');
}
