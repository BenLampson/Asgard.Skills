## C# 15 项目常用语言示例

### 优先使用新语法

| 特性 | 使用场景 | 示例 |
|------|----------|------|
| **Primary Constructors** | 构造注入作用域依赖 | `public class UserService(AbsAsgardContext ctx)` |
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
public class {ServiceName}(AbsAsgardContext asgardContext)
{
    protected AbsAsgardContext AsgardContext { get; } = asgardContext;
    protected {SettingsName} Settings => AsgardContext.Config.Get<{SettingsName}>();
    protected ILogger<{ServiceName}> Logger { get; } = asgardContext.Log.CreateLogger<{ServiceName}>();
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
