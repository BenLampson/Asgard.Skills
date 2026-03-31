namespace Asgard.Abstractions.Data;

/// <summary>
/// 仓储特性，用于标记仓储类以便自动扫描和依赖注入注册。
/// </summary>
/// <remarks>
/// <para>应用此特性后，框架会在启动时自动扫描并注册到依赖注入容器。</para>
/// <para>注册的生命周期默认为 Scoped，适用于每次请求。</para>
/// </remarks>
/// <example>
/// <para>使用示例：</para>
/// <code>
/// [Repository]
/// public class UserRepository : AbsAsgardRepositoryBase&lt;User, Guid&gt;
/// {
///     public UserRepository(IFreeSql fsql, IMultiLevelCache cache, ILogger&lt;UserRepository&gt; logger)
///         : base(fsql, cache, logger) { }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RepositoryAttribute : Attribute
{
}
