# .NET 10 Infrastructure Patterns

## Dependency Injection

### Service Lifetime Cheat Sheet

| Lifetime | When to use |
|----------|-------------|
| **Singleton** | Stateful objects that live for app lifetime |
| **Scoped** | Per-request services, database contexts |
| **Transient** | Lightweight stateless services created every time |

### Key Registration Patterns

```csharp
// Interface + implementation
services.AddScoped<IUserService, UserService>();

// Single concrete type without interface
services.AddScoped<UserService>();

// Factory
services.AddScoped<UserService>(sp =>
    new UserService(sp.GetRequiredService<IOptions<Settings>>()));

// Keyed services
services.AddKeyedScoped<IHandler, CreateHandler>("Create");
services.AddKeyedScoped<IHandler, DeleteHandler>("Delete");

// Options pattern
services.Configure<Settings>(configuration.GetSection("Settings"));
// Inject via IOptions<Settings>
```

### Validate On Start

Always add `ValidateOnStart()` to configurations that need it:

```csharp
builder.Services.AddOptions<DatabaseSettings>()
    .Bind(builder.Configuration.GetSection("Database"))
    .ValidateOnStart();
```

## Health Checks

Add health checks for probing:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddRedis("{redisConnectionString}")
    .AddUrlGroup(new Uri("https://external-api.com/health"));

app.MapHealthChecks("/health");
```

## Logging

Inject `ILogger<T>` where `T` is the class:

```csharp
public class UserService(ILogger<UserService> logger)
{
    // Use logger.LogInformation, logger.LogWarning, etc.
}
```

Use structured logging:

```csharp
logger.LogInformation("User {UserId} created at {CreatedAt}", userId, createdAt);
```

**Not**:

```csharp
logger.LogInformation($"User {userId} created at {createdAt}");
```

## HTTP Client

Always use `IHttpClientFactory`:

```csharp
// Named client
builder.Services.AddHttpClient("GitHub", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp");
})
.AddStandardResilienceHandler();

// Inject via IHttpClientFactory:
var client = _httpClientFactory.CreateClient("GitHub");

// Typed client (preferred for consumption)
builder.Services.AddHttpClient<IGitHubClient, GitHubClient>()
    .AddStandardResilienceHandler();
```

## Configuration

Bind configuration to options:

```csharp
builder.Services.AddOptions<MySettings>()
    .Bind(builder.Configuration.GetSection("MySettings"))
    .Validate(x =>
    {
        if (x.Enabled && string.IsNullOrEmpty(x.ConnectionString))
            return false;
        return true;
    }, "ConnectionString is required when Enabled is true")
    .ValidateOnStart();
```

Inject via `IOptions<MySettings>`:

```csharp
public class MyService(IOptions<MySettings> settings)
{
    private readonly MySettings _settings = settings.Value;
}
```

## Caching

### In-memory caching

```csharp
builder.Services.AddMemoryCache();

// Inject IMemoryCache
public class MyService(IMemoryCache cache)
{
    public async Task<Data> GetDataAsync()
    {
        var cached = await cache.GetAsync<Data>("key");
        if (cached != null) return cached;

        var data = await LoadFromDb();
        await cache.SetAsync("key", data, TimeSpan.FromMinutes(5));
        return data;
    }
}
```

### Output caching

```csharp
builder.Services.AddOutputCache();

app.UseOutputCache();

app.MapGet("/products", [OutputCache(Duration = 300)] () => { ... });
```

## Response Compression

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

app.UseResponseCompression();
```

## Resilience Pipeline

Built-in .NET 8+ resilience via `Microsoft.Extensions.Http.Resilience`:

```csharp
builder.Services.AddHttpClient("MyClient")
    .AddStandardResilienceHandler();
```

Custom pipeline:

```csharp
builder.Services.AddResiliencePipeline<string, HttpResponseMessage>("my-pipeline", pipeline =>
{
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});
```

## Background Services / Hosted Services

For long-running background tasks:

```csharp
public class MyBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Do work
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

// Registration
builder.Services.AddHostedService<MyBackgroundService>();
```

For queue-based background processing: Use `Channels`.

## Channels for Producer/Consumer

Use `System.Threading.Channels` for in-process async queues:

```csharp
var channel = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
{
    SingleReader = true,
    SingleWriter = false
});

// Producer
await channel.Writer.WriteAsync(item);

// Consumer (in hosted service)
await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
{
    await ProcessItem(item);
}
```

## Keyed DI for Strategy Pattern

When multiple strategies implement same interface:

```csharp
// Registration
builder.Services.AddKeyedSingleton<IPaymentProcessor, CreditCardProcessor>("CreditCard");
builder.Services.AddKeyedSingleton<IPaymentProcessor, PayPalProcessor>("PayPal");

// Resolution
public class PaymentService([FromKeyedServices("CreditCard")] IPaymentProcessor processor)
{
}

// Or from service provider
var processor = sp.GetRequiredKeyedService<IPaymentProcessor>(method);
```

## Summary

| Area | Pattern |
|------|---------|
| DI | Singleton/Scoped/Transient, use `ValidateOnStart()` |
| HTTP | `IHttpClientFactory` + `AddStandardResilienceHandler()` |
| Configuration | Options pattern, bind from configuration |
| Resilience | Use built-in `Microsoft.Extensions.Http.Resilience` |
| Background Tasks | `BackgroundService`, `Channels` for queues |
| Strategies | Use keyed DI |

## References

- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Configuration in .NET](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Resilience in .NET](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [Channels in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)
