namespace Asgard.Core.SystemConfig;

/// <summary>
/// 从多个属性源构建合并后的 Asgard 配置图。
/// </summary>
public sealed class AsgardConfigurationBuilder
{
    private readonly Dictionary<object, object> _data = new();

    /// <summary>
    /// 将 YAML 配置文件加入合并管线。
    /// </summary>
    /// <param name="path">YAML 文件路径。</param>
    /// <param name="optional">文件是否可选。</param>
    /// <returns>当前构建器。</returns>
    public AsgardConfigurationBuilder AddYamlFile(string path, bool optional = false)
    {
        if (!File.Exists(path))
        {
            if (optional)
            {
                return this;
            }

            throw new FileNotFoundException($"Configuration file not found: {path}", path);
        }

        // 后加入的数据源覆盖先加入的数据源，遵循常见配置系统的“后者优先”约定。
        AsgardConfigurationRoot.MergeInto(_data, YamlConfigLoader.LoadDictionaryFromFile(path));
        return this;
    }

    /// <summary>
    /// 将进程环境变量加入合并管线。
    /// </summary>
    /// <param name="prefix">可选的变量前缀，绑定前会先裁剪。</param>
    /// <returns>当前构建器。</returns>
    public AsgardConfigurationBuilder AddEnvironmentVariables(string? prefix = null)
    {
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is not string rawKey)
            {
                continue;
            }

            // prefix 既用于过滤环境变量，也用于去掉外层命名空间，便于映射到配置根节点。
            if (!string.IsNullOrEmpty(prefix))
            {
                if (!rawKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                rawKey = rawKey[prefix.Length..];
            }

            AsgardConfigurationRoot.SetValueByPath(_data, rawKey, entry.Value?.ToString());
        }

        return this;
    }

    /// <summary>
    /// 将命令行参数加入合并管线。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <returns>当前构建器。</returns>
    public AsgardConfigurationBuilder AddCommandLine(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        foreach (var (key, value) in ParseCommandLine(args))
        {
            // 命令行参数优先级最高，因此最后写入配置图。
            AsgardConfigurationRoot.SetValueByPath(_data, key, value);
        }

        return this;
    }

    /// <summary>
    /// 构建合并后的配置根对象。
    /// </summary>
    /// <returns>合并后的配置根对象。</returns>
    public AsgardConfigurationRoot Build()
    {
        return new AsgardConfigurationRoot(_data);
    }

    private static IEnumerable<KeyValuePair<string, string?>> ParseCommandLine(IEnumerable<string> args)
    {
        string? pendingKey = null;

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            if (TrySplitAssignment(arg, out var key, out var value))
            {
                if (!string.IsNullOrEmpty(pendingKey))
                {
                    // 独立旗标没有显式值时按 true 处理，兼容常见 CLI 约定。
                    yield return new KeyValuePair<string, string?>(pendingKey, "true");
                    pendingKey = null;
                }

                yield return new KeyValuePair<string, string?>(key, value);
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal) || arg.StartsWith("/", StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(pendingKey))
                {
                    // 遇到下一个 key 时，上一个仍未配值的旗标也按 true 收束。
                    yield return new KeyValuePair<string, string?>(pendingKey, "true");
                }

                pendingKey = arg.TrimStart('-', '/');
                continue;
            }

            if (!string.IsNullOrEmpty(pendingKey))
            {
                yield return new KeyValuePair<string, string?>(pendingKey, arg);
                pendingKey = null;
            }
        }

        if (!string.IsNullOrEmpty(pendingKey))
        {
            // 收尾处理最后一个无显式值的布尔旗标。
            yield return new KeyValuePair<string, string?>(pendingKey, "true");
        }
    }

    private static bool TrySplitAssignment(string arg, out string key, out string? value)
    {
        var separatorIndex = arg.IndexOf('=');
        if (separatorIndex < 0)
        {
            key = string.Empty;
            value = null;
            return false;
        }

        key = arg[..separatorIndex].Trim().TrimStart('-', '/');
        value = arg[(separatorIndex + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(key);
    }
}
