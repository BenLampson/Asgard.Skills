# .NET 10 Minimal APIs Best Practices

## Basic Endpoint Structure

### Top-level Statements

Keep `Program.cs` clean with top-level statements:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services

var app = builder.Build();

// Configure middleware

app.Run();
```

### `Program` class partial definition

The C# compiler generates the `Program` class. Expose it for WebApplicationFactory:

```csharp
// Program.cs
// ... (top-level code)

public partial class Program { } // Empty partial for test projects
```

## Typed Results

Prefer `TypedResults` over `Results` for better type inference:

```csharp
// Good
app.MapGet("/users/{id}", async (int id, IUserService service) =>
{
    var user = await service.GetByIdAsync(id);
    return user is not null
        ? TypedResults.Ok(user)
        : TypedResults.NotFound();
});

// Avoid
app.MapGet("/users/{id}", async (int id, IUserService service) =>
{
    var user = await service.GetByIdAsync(id);
    return user is not null
        ? Results.Ok(user)
        : Results.NotFound();
});
```

## Route Groups

Organize related endpoints with route groups:

```csharp
var users = app.MapGroup("/api/users")
    .WithTags("Users");

users.MapGet("/", GetAllUsers);
users.MapGet("/{id}", GetUserById);
users.MapPost("/", CreateUser);
users.MapPut("/{id}", UpdateUser);
users.MapDelete("/{id}", DeleteUser);
```

Use groups for common metadata:

```csharp
var api = app.MapGroup("/api")
    .RequireAuthorization()
    .WithOpenApi();

api.MapGet("/health", () => Results.Ok());
```

## Endpoint Filters

Use endpoint filters for cross-cutting concerns like validation:

```csharp
public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices
            .GetService<IValidator<T>>();
        
        if (validator is null)
        {
            return await next(context);
        }

        var input = context.Arguments.OfType<T>().First();
        var result = await validator.ValidateAsync(input);
        
        if (!result.IsValid)
        {
            return TypedResults.UnprocessableEntity(result.Errors);
        }

        return await next(context);
    }
}

// Usage
builder.Services.AddScoped<ValidationFilter<CreateUserCommand>>();

app.MapPost("/users", async (CreateUserCommand cmd, IUserService service) =>
{
    var user = await service.Create(cmd);
    return TypedResults.Created($"/users/{user.Id}", user);
})
.AddEndpointFilter<ValidationFilter<CreateUserCommand>>();
```

## Parameter Binding

Use explicit binding attributes for clarity:

```csharp
app.MapGet("/users/{id}", async (
    [FromRoute] int id,
    [FromQuery] string search,
    [FromServices] IUserService service) =>
{
    // ...
});
```

## OpenAPI / Swagger

Enable OpenAPI with `builder.AddOpenApi()`:

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// OR (NET 10+)
builder.AddOpenApi();

// ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Add descriptive metadata:

```csharp
app.MapGet("/users/{id}", async (int id, IUserService service) => { ... })
   .WithName("GetUserById")
   .WithSummary("Gets a user by ID")
   .WithDescription("Returns detailed user information including profile")
   .Produces<User>(StatusCodes.Status200OK)
   .ProducesProblem(StatusCodes.Status404NotFound);
```

## Http Resilience

Add standard resilience to HTTP clients:

```csharp
builder.Services.AddHttpClient<IApiClient, ApiClient>()
    .AddStandardResilienceHandler();
```

This includes:
- Retry with exponential backoff
- Circuit breaker
- Rate limiter
- Timeout

## Structure for Large Apps

When the app grows, organize endpoints by feature:

```csharp
// Features/Users/UsersEndpoints.cs
namespace MyApp.Features.Users;

public static class UsersEndpoints
{
    public static void MapUsers(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users")
            .WithTags("Users");

        group.MapGet("/", GetAllUsers);
        group.MapGet("/{id}", GetUserById);
        // ...
    }
}

// Program.cs
app.MapUsers();
```

## Summary Table

| Concept | Prefer | Avoid |
|---------|--------|-------|
| Results | `TypedResults` | `Results` |
| Organization | Route groups / extension methods | All endpoints in Program.cs |
| DI | Constructor injection in services | `builder.Services.BuildServiceProvider()` |
| Validation | Endpoint filters / FluentValidation | Manual validation in each endpoint |
| Resilience | `AddStandardResilienceHandler()` | Manual Polly configuration |
| Metadata | `WithOpenApi()`, `WithTags()`, `Produces()` | Missing documentation |

## References

- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Minimal APIs Fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview)
- [Route Groups](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-groups)
