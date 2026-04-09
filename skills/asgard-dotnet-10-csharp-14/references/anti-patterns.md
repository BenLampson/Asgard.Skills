# Anti-Patterns to Avoid

## Quick Reference: What to Use Instead

| ❌ Anti-Pattern | ✅ Use Instead |
|----------------|----------------|
| `new HttpClient()` | Inject `HttpClient` or `IHttpClientFactory` |
| `Results.Ok()` | `TypedResults.Ok()` |
| Manual Polly config | `AddStandardResilienceHandler()` |
| `DateTime.Now` | `DateTime.UtcNow` |
| `GetAsync().Result` | `await GetAsync()` |
| Exceptions for flow control | `ErrorOr<T>` / Result pattern |
| Manual backing fields | C# 14 `field` keyword |
| Traditional extension methods | C# 14 extension blocks |
| `if (x != null) x.Prop = y` | `x?.Prop = y` (null-conditional assignment) |
| Missing `ValidateOnStart()` | Always add `.ValidateOnStart()` |
| Singleton → Scoped injection | Use `IServiceScopeFactory` |
| `_count++` in singleton | `Interlocked.Increment(ref _count)` |
| `dto.ToEntity()` 后直接 `UpdateAsync(entity)` | 先查询当前实体，再在原实体上变更并更新 |

---

## HttpClient Misuse

```csharp
// WRONG: Creates socket exhaustion
public async Task Bad()
{
    using var client = new HttpClient(); // Don't instantiate per-request
    await client.GetAsync("...");
}

// CORRECT: Inject IHttpClientFactory or typed client
public class MyService(HttpClient client)
{
    public Task<string> GetAsync() => client.GetStringAsync("...");
}

// CORRECT: Named client
public class MyService(IHttpClientFactory factory)
{
    public async Task<string> GetAsync()
    {
        var client = factory.CreateClient("ExternalApi");
        return await client.GetStringAsync("...");
    }
}
```

---

## DI Captive Dependencies

```csharp
// WRONG: Singleton captures scoped service (memory leak)
public class SingletonService(IScopedService scoped) { }

// CORRECT: Inject IServiceScopeFactory
public class SingletonService(IServiceScopeFactory factory)
{
    public async Task DoWork()
    {
        await using var scope = factory.CreateAsyncScope();
        var scoped = scope.ServiceProvider.GetRequiredService<IScopedService>();
        // Use scoped service
    }
}

// CORRECT: Use IServiceProvider for optional resolution
public class SingletonService(IServiceProvider sp)
{
    public async Task DoWork()
    {
        await using var scope = sp.CreateAsyncScope();
        var scoped = scope.ServiceProvider.GetRequiredService<IScopedService>();
    }
}
```

---

## Exceptions as Flow Control

```csharp
// WRONG: Expensive, hard to trace
public async Task<IResult> GetUser(int id)
{
    try
    {
        var user = await _service.GetUserAsync(id);
        return TypedResults.Ok(user);
    }
    catch (UserNotFoundException)
    {
        return TypedResults.NotFound();
    }
}

// CORRECT: Result pattern with ErrorOr
public async Task<IResult> GetUser(int id)
{
    var result = await _service.GetUserAsync(id);
    return result.Match(
        user => TypedResults.Ok(user),
        errors => TypedResults.NotFound()
    );
}

// CORRECT: Nullable return
public async Task<IResult> GetUser(int id)
{
    var user = await _service.GetUserAsync(id);
    return user is not null
        ? TypedResults.Ok(user)
        : TypedResults.NotFound();
}
```

---

## Blocking Async

```csharp
// WRONG: Deadlock risk
var result = GetAsync().Result;
var result2 = GetAsync().GetAwaiter().GetResult();
GetAsync().Wait();

// CORRECT: Async all the way
var result = await GetAsync();

// If you MUST block (rare), use this pattern
var result = Task.Run(() => GetAsync()).GetAwaiter().GetResult();
```

---

## N+1 Queries

```csharp
// WRONG: N+1 query problem
var orders = await db.Orders.ToListAsync();
foreach (var order in orders)
{
    var items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
}

// CORRECT: Include related data
var orders = await db.Orders
    .Include(o => o.Items)
    .ToListAsync();

// CORRECT: Explicit loading when needed
var order = await db.Orders.FindAsync(id);
await db.Entry(order).Collection(o => o.Items).LoadAsync();
```

---

## Optimistic Lock Update Misuse

```csharp
// WRONG: DTO 重建实体会丢失数据库当前 Version 和持久化字段
var entity = dto.ToEntity();
entity.Id = id;
await repository.UpdateAsync(entity);

// CORRECT: 先读取数据库当前实体, 再在原实体上修改
var entity = await repository.GetByIdAsync(id)
    ?? throw new InvalidOperationException($"未找到实体：{id}");

entity.Update(dto.Name, dto.Description);
await repository.UpdateAsync(entity);

// CORRECT: 没有行为方法时显式逐字段赋值, 并在需要时标记更新时间
var entity = await repository.GetByIdAsync(id)
    ?? throw new InvalidOperationException($"未找到实体：{id}");

entity.Name = dto.Name;
entity.Description = dto.Description;
entity.MarkAsUpdated();

await repository.UpdateAsync(entity);
```

适用规则:

- 只要实体继承 `AbsAsgardBaseEntity`、`AbsAsgardTenantEntity`、`AbsAsgardTenantUserDataEntity`, 就默认按乐观锁实体处理
- 只要存在 `Version` / `[Column(IsVersion = true)]`, 更新就必须采用“先查后改”
- `CreateTime`、`CreateBy`、`Deleted`、`TenantId`、`ClientId` 等持久化字段不能由 DTO 回填覆盖

---

## Async Void

```csharp
// WRONG: Exceptions can't be caught
public async void HandleEvent(object sender, EventArgs e)
{
    await ProcessAsync(); // If this throws, app may crash
}

// CORRECT: Return Task
public async Task HandleEventAsync(CancellationToken ct)
{
    await ProcessAsync(ct);
}

// If you must use event handlers, wrap in try-catch
public async void HandleEvent(object sender, EventArgs e)
{
    try
    {
        await ProcessAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Event handler failed");
    }
}
```

---

## Mutable Shared State

```csharp
// WRONG: Race condition in singleton
public class CounterService
{
    private int _count;
    public void Increment() => _count++; // Not thread-safe
}

// CORRECT: Use Interlocked
public class CounterService
{
    private int _count;
    public void Increment() => Interlocked.Increment(ref _count);
}

// CORRECT: Use ConcurrentDictionary for collections
public class CacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
}
```

---

## Swallowing Exceptions

```csharp
// WRONG: Silent failures
try
{
    await ProcessAsync();
}
catch { } // Never do this

// WRONG: Logging but not handling
try
{
    await ProcessAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed");
    // Continues as if nothing happened
}

// CORRECT: Log and rethrow or handle appropriately
try
{
    await ProcessAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    _logger.LogError(ex, "Processing failed for {ItemId}", item.Id);
    throw; // Or return error result
}
```

---

## String Concatenation in Loops

```csharp
// WRONG: O(n²) allocations
var result = "";
foreach (var item in items)
{
    result += item.ToString();
}

// CORRECT: StringBuilder
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.Append(item);
}
var result = sb.ToString();

// CORRECT: String.Join
var result = string.Join("", items);
```

---

## Disposing IDisposable Incorrectly

```csharp
// WRONG: May not dispose on exception
var stream = new FileStream(path, FileMode.Open);
// ... use stream
stream.Dispose();

// CORRECT: using statement
using var stream = new FileStream(path, FileMode.Open);
// ... use stream (disposed automatically)

// CORRECT: using block
using (var stream = new FileStream(path, FileMode.Open))
{
    // ... use stream
}
```

---

## Exposing Internal Implementation

```csharp
// WRONG: Exposes internal list
public class OrderService
{
    private readonly List<Order> _orders = new();
    public List<Order> GetOrders() => _orders; // Caller can modify!
}

// CORRECT: Return IReadOnlyList or copy
public class OrderService
{
    private readonly List<Order> _orders = new();
    public IReadOnlyList<Order> GetOrders() => _orders.AsReadOnly();
}
```

---

## DateTime.Now vs UTC

```csharp
// WRONG: Local time causes issues across timezones
var timestamp = DateTime.Now;

// CORRECT: Always use UTC for storage/comparison
var timestamp = DateTime.UtcNow;

// CORRECT: Use DateTimeOffset for timezone-aware scenarios
var timestamp = DateTimeOffset.UtcNow;
```
