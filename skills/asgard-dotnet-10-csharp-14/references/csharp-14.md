# C# 14 - Modern C# Features

Use these patterns for modern .NET 10 / C# 14 code.

## Primary Constructors

```csharp
// Good (C# 14 primary constructor)
public class UserService(IOptions<Settings> settings, ILogger<UserService> logger)
{
    private readonly Settings _settings = settings.Value;
    private readonly ILogger<UserService> _logger = logger;
}

// Older - avoid when possible (more verbose)
public class UserService
{
    private readonly Settings _settings;
    private readonly ILogger<UserService> _logger;

    public UserService(IOptions<Settings> settings, ILogger<UserService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }
}
```

## Collection Expressions

```csharp
// Good
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob", "Charlie"];
Dictionary<string, int> ages = new()
{
    ["Alice"] = 30,
    ["Bob"] = 25
};

// Spread with ..
List<int> combined = [.. first, .. second];

// Older - avoid
int[] numbers = new int[] { 1, 2, 3, 4, 5 };
var names = new List<string> { "Alice", "Bob", "Charlie" };
```

## `field` Keyword for Property Initialization

```csharp
// Good (C# 14 - no backing field needed)
public class Person
{
    public string Name
    {
        get;
        set => field = value.Trim();
    }
}

// Older - avoid (explicit backing field required)
public class Person
{
    private string _name;
    public string Name
    {
        get => _name;
        set => _name = value.Trim();
    }
}
```

## Extension Blocks (C# 14)

Declare extension blocks inside a top-level, nongeneric static class. Traditional `this` extension methods remain valid.

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

## File-scoped Namespaces

```csharp
// Good (file-scoped)
namespace MyApp.Features.Users;

public class UserService { }

// Older - avoid (block-scoped wastes vertical space)
namespace MyApp.Features.Users
{
    public class UserService { }
}
```

## File-scoped Namespaces

Always use file-scoped namespaces to save vertical space and reduce nesting.

```csharp
// CORRECT:
namespace MyFeature;

public class MyClass
{
    // ...
}

// AVOID:
namespace MyFeature
{
    public class MyClass
    {
        // ...
    }
}
```

## Nullable Reference Types

Enable `<Nullable>enable</Nullable>` and use nullable annotations:

```csharp
// Required optional parameter
public string? MiddleName { get; set; }

// Non-nullable after construction
public string FirstName { get; set; } = null!;

// Guard clause (modern pattern)
ArgumentNullException.ThrowIfNull(name);
```

## Null Conditional Assignment (C# 14)

```csharp
// C# 14 feature
user?.Name = "John"; // only assigns if user is not null

// Older equivalent
if (user != null)
    user.Name = "John";
```

## Interceptor Pattern (for advanced usage)

Interceptors are an experimental compiler feature, not a default C# 14 application pattern. Use only when a project explicitly requires the feature and its compiler configuration.

## Summary Table

| Feature | Usage | Example |
|---------|-------|---------|
| Primary Constructors | DI, simple types | `public class Service(ILogger log)` |
| Collection Expressions | Arrays, lists, dicts | `int[] x = [1, 2, 3];` |
| `field` keyword | Auto-properties with logic | `set => field = value.Trim()` |
| Extension Blocks | Organized extensions | `extension<T>(IQueryable<T> query)` |
| File-scoped Namespaces | Reduced nesting | `namespace Feature;` |
| Nullable Reference Types | Safety | `string?`, `null!`, `ArgumentNullException.ThrowIfNull` |
| Null Conditional Assignment | Shorter conditionals | `obj?.Prop = newValue` |

## References

- [What's new in C# 12](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)
- [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
