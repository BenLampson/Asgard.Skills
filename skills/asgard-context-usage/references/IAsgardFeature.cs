namespace Asgard.Abstractions;

/// <summary>业务能力的运行状态和只读诊断。直接注册的既有接口实现仍可独立使用。</summary>
public interface IAsgardFeature
{
    /// <summary>描述能力是否已装配并启用，单次调用失败不会关闭模块。</summary>
    AsgardFeatureStatus Status => AsgardFeatureStatus.Enabled;

    /// <summary>可对外显示的有效配置快照，省略或遮盖凭据。</summary>
    IReadOnlyDictionary<string, string> Configuration => System.Collections.ObjectModel.ReadOnlyDictionary<string, string>.Empty;

    /// <summary>宿主内最近一次能力调用的异常摘要，尚无失败时为空。</summary>
    AsgardFeatureError? LastError => null;
}
