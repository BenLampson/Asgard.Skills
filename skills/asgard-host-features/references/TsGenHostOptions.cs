namespace Asgard.Abstractions.AspNetCore.Host;

/// <summary>
/// TypeScript 客户端导出端点配置。
/// </summary>
public sealed class TsGenHostOptions
{
    /// <summary>
    /// 是否在开发环境启用 TypeScript 客户端导出端点。
    /// </summary>
    [ConfigPath("host.tsGen.enabled", DefaultValue = false)]
    public bool Enabled { get; set; }

    /// <summary>
    /// 校验 TypeScript 客户端导出配置。
    /// </summary>
    public void Validate()
    {
    }
}
