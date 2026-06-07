namespace Asgard.Core.SystemConfig;

/// <summary>
/// 表示合并后的 Asgard 配置图。
/// </summary>
public sealed class AsgardConfigurationRoot
{
    private static readonly Regex _indexedSegmentRegex = new(@"\[(\d+)\]", RegexOptions.Compiled);
    private static readonly Regex _placeholderRegex = new(@"\$\{([^}]+)\}", RegexOptions.Compiled);
    private readonly Dictionary<object, object> _data;

    internal AsgardConfigurationRoot(Dictionary<object, object> data)
    {
        _data = CloneDictionary(data);
    }

    /// <summary>
    /// 从合并后的配置图加载强类型配置对象。
    /// </summary>
    /// <typeparam name="T">配置对象类型。</typeparam>
    /// <returns>填充完成的配置对象。</returns>
    public T Load<T>() where T : new()
    {
        return ConfigurationConfigLoader.Load<T>(CreateResolvedSnapshot());
    }

    /// <summary>
    /// 从指定配置节加载强类型对象。
    /// </summary>
    /// <typeparam name="T">配置节对象类型。</typeparam>
    /// <param name="sectionPath">配置节路径，支持点号、冒号或双下划线分隔。</param>
    /// <returns>填充完成的配置节对象。</returns>
    public T LoadSection<T>(string sectionPath) where T : new()
    {
        return ConfigurationConfigLoader.LoadSection<T>(CreateResolvedSnapshot(), sectionPath);
    }

    /// <summary>
    /// 以冒号分隔的键值对形式暴露合并后的配置，便于兼容 ASP.NET 配置系统。
    /// </summary>
    /// <returns>拍平后的配置字典。</returns>
    public IReadOnlyDictionary<string, string?> ToConfigurationDictionary()
    {
        var flattened = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        Flatten(CreateResolvedSnapshot(), flattened);
        return flattened;
    }

    internal Dictionary<object, object> CreateResolvedSnapshot()
    {
        var snapshot = CloneDictionary(_data);
        // 先克隆再解析占位符，避免污染原始配置图并支持重复加载不同节对象。
        _ = ResolvePlaceholders(snapshot, snapshot, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return snapshot;
    }

    internal static string NormalizePath(string path)
    {
        // 统一把环境变量、命令行和配置系统里的不同分隔风格归一成点号路径。
        var normalized = path.Trim().TrimStart('-', '/');
        normalized = normalized.Replace("__", ".", StringComparison.Ordinal);
        normalized = normalized.Replace(':', '.');
        normalized = _indexedSegmentRegex.Replace(normalized, ".$1");
        normalized = normalized.Replace("..", ".", StringComparison.Ordinal);
        return normalized.Trim('.');
    }

    internal static void MergeInto(Dictionary<object, object> target, Dictionary<object, object> source)
    {
        foreach (var (sourceKey, sourceValue) in source)
        {
            // 使用忽略大小写的 key 合并，避免不同来源仅因大小写不同形成重复节点。
            var existingKey = FindExistingKey(target, sourceKey.ToString());

            if (sourceValue is Dictionary<object, object> sourceDict)
            {
                if (existingKey != null && target[existingKey] is Dictionary<object, object> targetDict)
                {
                    MergeInto(targetDict, sourceDict);
                }
                else
                {
                    target[existingKey ?? sourceKey] = CloneDictionary(sourceDict);
                }

                continue;
            }

            if (sourceValue is List<object> sourceList)
            {
                target[existingKey ?? sourceKey] = CloneList(sourceList);
                continue;
            }

            target[existingKey ?? sourceKey] = sourceValue!;
        }
    }

    internal static void SetValueByPath(Dictionary<object, object> target, string path, string? value)
    {
        var normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return;
        }

        // 规范化后再分段写入，确保命令行和环境变量都能落到统一的树形结构上。
        var segments = normalizedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        SetValueInDictionary(target, segments, 0, value);
    }

    internal static Dictionary<object, object> CloneDictionary(Dictionary<object, object> source)
    {
        var clone = new Dictionary<object, object>();

        foreach (var (key, value) in source)
        {
            clone[key] = value switch
            {
                Dictionary<object, object> nestedDict => CloneDictionary(nestedDict),
                List<object> list => CloneList(list),
                _ => value
            };
        }

        return clone;
    }

    private static List<object> CloneList(List<object> source)
    {
        var clone = new List<object>(source.Count);

        foreach (var item in source)
        {
            clone.Add(item switch
            {
                Dictionary<object, object> nestedDict => CloneDictionary(nestedDict),
                List<object> nestedList => CloneList(nestedList),
                _ => item!
            });
        }

        return clone;
    }

    private static void SetValueInDictionary(Dictionary<object, object> target, string[] segments, int index, string? value)
    {
        var key = segments[index];
        var existingKey = FindExistingKey(target, key) ?? key;

        if (index == segments.Length - 1)
        {
            target[existingKey] = value ?? string.Empty;
            return;
        }

        var nextIsIndex = int.TryParse(segments[index + 1], out _);
        if (!target.TryGetValue(existingKey, out var child))
        {
            // 预读下一段是否为索引，以决定当前节点应创建字典还是列表。
            child = nextIsIndex ? new List<object>() : new Dictionary<object, object>();
            target[existingKey] = child;
        }

        if (nextIsIndex)
        {
            if (child is not List<object> list)
            {
                list = new List<object>();
                target[existingKey] = list;
            }

            SetValueInList(list, segments, index + 1, value);
            return;
        }

        if (child is not Dictionary<object, object> dict)
        {
            dict = new Dictionary<object, object>();
            target[existingKey] = dict;
        }

        SetValueInDictionary(dict, segments, index + 1, value);
    }

    private static void SetValueInList(List<object> target, string[] segments, int index, string? value)
    {
        var listIndex = int.Parse(segments[index]);

        // 自动扩容列表，保证通过路径写入任意索引时中间节点也能被补齐。
        while (target.Count <= listIndex)
        {
            target.Add(new Dictionary<object, object>());
        }

        if (index == segments.Length - 1)
        {
            target[listIndex] = value ?? string.Empty;
            return;
        }

        var nextIsIndex = int.TryParse(segments[index + 1], out _);
        var child = target[listIndex];

        if (nextIsIndex)
        {
            if (child is not List<object> nextList)
            {
                nextList = new List<object>();
                target[listIndex] = nextList;
            }

            SetValueInList(nextList, segments, index + 1, value);
            return;
        }

        if (child is not Dictionary<object, object> dict)
        {
            dict = new Dictionary<object, object>();
            target[listIndex] = dict;
        }

        SetValueInDictionary(dict, segments, index + 1, value);
    }

    private static object? ResolvePlaceholders(object? value, Dictionary<object, object> root, HashSet<string> stack)
    {
        return value switch
        {
            Dictionary<object, object> dict => ResolveDictionaryPlaceholders(dict, root, stack),
            List<object> list => ResolveListPlaceholders(list, root, stack),
            string text => ResolveStringPlaceholders(text, root, stack),
            _ => value
        };
    }

    private static Dictionary<object, object> ResolveDictionaryPlaceholders(
        Dictionary<object, object> dict,
        Dictionary<object, object> root,
        HashSet<string> stack)
    {
        var keys = dict.Keys.ToList();
        foreach (var key in keys)
        {
            dict[key] = ResolvePlaceholders(dict[key], root, stack)!;
        }

        return dict;
    }

    private static List<object> ResolveListPlaceholders(
        List<object> list,
        Dictionary<object, object> root,
        HashSet<string> stack)
    {
        for (var i = 0; i < list.Count; i++)
        {
            list[i] = ResolvePlaceholders(list[i], root, stack)!;
        }

        return list;
    }

    private static string ResolveStringPlaceholders(string value, Dictionary<object, object> root, HashSet<string> stack)
    {
        return _placeholderRegex.Replace(value, match =>
        {
            var placeholder = match.Groups[1].Value.Trim();
            if (TryResolveEnvironmentPlaceholder(placeholder, out var environmentValue))
            {
                return environmentValue;
            }

            var path = NormalizePath(placeholder);
            if (!stack.Add(path))
            {
                // 通过 stack 检测占位符递归引用，避免无限展开。
                throw new InvalidOperationException($"检测到循环配置占位符引用: {path}");
            }

            try
            {
                var resolved = YamlPathResolver.GetValueFromPathIgnoreCase(root, path);
                if (resolved is null)
                {
                    return match.Value;
                }

                return resolved switch
                {
                    // 如果目标值本身仍包含占位符，则继续递归展开直到得到最终文本。
                    string text => ResolveStringPlaceholders(text, root, stack),
                    _ => resolved.ToString() ?? string.Empty
                };
            }
            finally
            {
                _ = stack.Remove(path);
            }
        });
    }

    private static bool TryResolveEnvironmentPlaceholder(string placeholder, out string value)
    {
        const string environmentPrefix = "env:";
        value = string.Empty;

        if (!placeholder.StartsWith(environmentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var variableName = placeholder[environmentPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(variableName))
        {
            throw new InvalidOperationException("环境变量占位符名称不能为空");
        }

        value = Environment.GetEnvironmentVariable(variableName)
            ?? throw new InvalidOperationException($"环境变量未设置: {variableName}");
        return true;
    }

    private static void Flatten(
        Dictionary<object, object> source,
        Dictionary<string, string?> result,
        string? prefix = null)
    {
        foreach (var (key, value) in source)
        {
            var segment = key.ToString() ?? string.Empty;
            var fullKey = string.IsNullOrEmpty(prefix) ? segment : $"{prefix}:{segment}";

            switch (value)
            {
                case Dictionary<object, object> dict:
                    Flatten(dict, result, fullKey);
                    break;
                case List<object> list:
                    FlattenList(list, result, fullKey);
                    break;
                default:
                    result[fullKey] = value?.ToString();
                    break;
            }
        }
    }

    private static void FlattenList(List<object> source, Dictionary<string, string?> result, string prefix)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var fullKey = $"{prefix}:{i}";
            switch (source[i])
            {
                case Dictionary<object, object> dict:
                    Flatten(dict, result, fullKey);
                    break;
                case List<object> list:
                    FlattenList(list, result, fullKey);
                    break;
                default:
                    result[fullKey] = source[i]?.ToString();
                    break;
            }
        }
    }

    private static object? FindExistingKey(Dictionary<object, object> dict, string? key)
    {
        if (key is null)
        {
            return null;
        }

        foreach (var existingKey in dict.Keys)
        {
            if (string.Equals(existingKey?.ToString(), key, StringComparison.OrdinalIgnoreCase))
            {
                return existingKey;
            }
        }

        return null;
    }
}
