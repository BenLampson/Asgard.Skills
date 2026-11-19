namespace Asgard.Abstractions.SystemConfig;

/// <summary>读取当前作用域已装配的宿主和插件配置，并查看可公开的诊断摘要。</summary>
public interface IAsgardConfig : ISystemConfig
{
    /// <summary>取得容器中绑定完成的配置实例。返回值供业务使用，可能包含凭据；展示配置使用 Configuration。</summary>
    /// <typeparam name="TConfig">通过宿主或插件约定注册的强类型配置。</typeparam>
    /// <exception cref="InvalidOperationException">已启用配置能力，但没有注册该类型。</exception>
    TConfig Get<TConfig>() where TConfig : class, ISystemConfig;
}
