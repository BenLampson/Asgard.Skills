## C# 14 语言特性使用指南

### 优先使用新语法

| 特性 | 使用场景 | 示例 |
|------|----------|------|
| **Primary Constructors** | 依赖注入注入、简单类型 | `public class UserService(IOptions<Settings> settings, ILogger<UserService> logger)` |
| **Collection Expressions** | 数组、列表、字典初始化 | `int[] numbers = [1, 2, 3];` |
| **`field` keyword** | 自动属性带逻辑 | `set => field = value.Trim();` |
| **Extension Blocks** | 扩展方法组织 | `extension<T>(IQueryable<T> query)`（位于 static class 内） |
| **File-scoped Namespaces** | 减少嵌套 | `namespace MyFeature;` |
| **Nullable Reference Types** | 空安全 | `string?`, `null!`, `ArgumentNullException.ThrowIfNull` |
| **Null Conditional Assignment** | 简写条件赋值 | `user?.Name = "John";` |

### 代码示例：主构造函数

```csharp
namespace {Namespace};

/// <summary>
/// {ServiceSummary}
/// </summary>
public class {ServiceName}(
    IOptions<{SettingsName}> settings,
    ILogger<{ServiceName}> logger,
    AbsAsgardContext asgardContext)
{
    private readonly {SettingsType} _settings = settings.Value;
    private readonly ILogger<{ServiceName}> _logger = logger;
    protected readonly AbsAsgardContext AsgardContext = asgardContext;
}
```

### 代码示例：扩展块

```csharp
public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    {
        /// <summary>按条件追加查询过滤。</summary>
        public IQueryable<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
            => condition ? query.Where(predicate) : query;

        /// <summary>构建分页查询；异步执行使用项目 ORM 提供的 API。</summary>
        public IQueryable<T> Page(int page, int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
```
