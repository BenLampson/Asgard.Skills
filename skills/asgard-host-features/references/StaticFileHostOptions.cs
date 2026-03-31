namespace Asgard.Abstractions.AspNetCore.Host;

/// <summary>
/// 静态文件服务配置选项。
/// </summary>
/// <remarks>
/// 用于控制宿主是否启用静态文件映射，以及静态资源目录与请求前缀。
/// 默认会把内容根目录下的 <c>wwwroot</c> 暴露为站点静态资源目录。
/// </remarks>
public class StaticFileHostOptions
{
    /// <summary>
    /// 是否启用静态文件映射。
    /// </summary>
    /// <remarks>
    /// 为 <see langword="true" /> 时，宿主会注册静态文件中间件；
    /// 为 <see langword="false" /> 时，不会暴露任何静态目录。
    /// </remarks>
    [ConfigPath("host.staticFiles.enabled", DefaultValue = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 静态资源根目录。
    /// </summary>
    /// <remarks>
    /// 支持相对路径和绝对路径。
    /// 相对路径会基于应用内容根目录解析，默认值为 <c>wwwroot</c>。
    /// </remarks>
    [ConfigPath("host.staticFiles.webRootPath", DefaultValue = "wwwroot")]
    public string WebRootPath { get; set; } = "wwwroot";

    /// <summary>
    /// 静态文件请求前缀。
    /// </summary>
    /// <remarks>
    /// 为空字符串时表示直接从站点根路径提供静态资源；
    /// 例如设置为 <c>/assets</c> 后，可通过 <c>/assets/app.js</c> 访问资源。
    /// </remarks>
    [ConfigPath("host.staticFiles.requestPath", DefaultValue = "")]
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用默认文件（index.html 等）。
    /// </summary>
    /// <value>true 启用，false 禁用，默认为 true。</value>
    [ConfigPath("enableDefaultFiles", DefaultValue = true)]
    public bool EnableDefaultFiles { get; set; } = true;

    /// <summary>
    /// 校验静态文件配置项有效性。
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置值不合法时抛出。</exception>
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(WebRootPath))
        {
            throw new InvalidOperationException("静态文件根目录不能为空。");
        }

        if (RequestPath.Length > 0 && !RequestPath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("静态文件请求前缀必须以 / 开头，或使用空字符串。");
        }
    }
}
