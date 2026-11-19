namespace Asgard.Core.SystemConfig;

public sealed partial class AsgardConfigurationRoot
{
    /// <summary>宿主选择环境文件时只展开环境名，其他配置项等全部来源合并后再展开。</summary>
    internal string? GetScalar(string path)
    {
        var value = YamlPathResolver.GetValueFromPathIgnoreCase(_data, path);
        return value is string text
            ? ResolveStringPlaceholders(text, _data, new HashSet<string>(StringComparer.OrdinalIgnoreCase))
            : value?.ToString();
    }

    /// <summary>映射允许其他来源补充子键，标量和列表则完整替换对应节点及其子树。</summary>
    internal bool OwnsConfigurationPath(string path, out bool replacesChildren)
    {
        object? node = _data;
        foreach (var segment in path.Split(':'))
        {
            if (node is not Dictionary<object, object> mapping) break;
            if (mapping.TryGetValue(segment, out node)) continue;
            var key = FindExistingKey(mapping, segment);
            if (key is null)
            {
                replacesChildren = false;
                return false;
            }
            node = mapping[key];
        }
        replacesChildren = node is not Dictionary<object, object>;
        return true;
    }
}
