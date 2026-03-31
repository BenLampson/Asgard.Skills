# .NET 10 Security Best Practices

## Authentication with ASP.NET Core Identity

Basic setup:

```csharp
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<AppDbContext>();
```

## JWT Authentication

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
```

## Authorization

### Role-based

```csharp
[Authorize(Roles = "Admin")]
app.MapGet("/admin/users", () => { ... });

// Or
app.MapGet("/admin/users", () => { ... })
   .RequireAuthorization("Admin");
```

### Policy-based

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CanEdit", policy =>
        policy.RequireClaim("Permission", "Edit"));
});
```

### Resource-based

```csharp
public class DocumentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Document resource)
    {
        if (context.User.IsInRole("Admin") ||
            resource.OwnerId == context.User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

## Data Protection

Persist keys to storage:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/app/keys"))
    .SetApplicationName("MyApp");
```

For multi-machine:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToRedis(redisConnectionMultiplexer, "DataProtection-Keys")
    .SetApplicationName("MyApp");
```

## HTTPS/SSL

Always enforce HTTPS in production:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

## CORS

Configure CORS properly:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("https://yourfrontend.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// In middleware
app.UseCors("AllowSpecificOrigin");
```

**Avoid**: `AllowAnyOrigin()` with `AllowCredentials()` - it's insecure.

## Secure Configuration

Never commit secrets to source control:

- Use `user-secrets` in development
- Use environment variables in CI/CD
- Use Azure Key Vault / AWS Secrets Manager in production

```csharp
// Use configuration from key vault
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:Uri"]!),
    new DefaultAzureCredential());
```

## Open Redirect Protection

Never redirect to an unvalidated URL:

```csharp
// Bad (vulnerable to open redirect)
return Redirect(url);

// Good (validate it's an application-local path)
if (Url.IsLocalUrl(returnUrl))
{
    return Redirect(returnUrl);
}
```

## Mass Assignment / Over-Posting

Explicitly include only what can be bound:

```csharp
// Good: use a DTO with only allowed properties
public class CreateUserCommand
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    // Not: public bool IsAdmin { get; set; }  <-- client could set this
}
```

Avoid mapping directly from user input to entity:

```csharp
// Bad: user could set IsAdmin
dbContext.Users.Update(inputModel);
await dbContext.SaveChangesAsync();
```

## SQL Injection

**Always** use parameterized queries with EF Core or Dapper:

```csharp
// Good (EF Core parameterizes automatically)
var users = await db.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// Good (Dapper parameters)
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Email = @Email",
    new { Email = email });

// Bad: avoid string interpolation for queries
var user = await connection.QueryFirstOrDefaultAsync<User>(
    $"SELECT * FROM Users WHERE Email = '{email}'"); // INSECURE
```

## CSRF / XSRF Protection

For cookies-based authentication, enable anti-forgery:

```csharp
builder.Services.AddAntiforgery();

// In forms:
<form asp-action="Submit">
    @Html.AntiForgeryToken()
</form>
```

## XSS Prevention

In Razor, output is automatically encoded:

```razor
<!-- Automatic HTML encoding: safe -->
<div>@userInput</div>

<!-- If you must output raw HTML, be sure it's safe -->
<div>@Html.Raw(safeHtml)</div>
```

Never output unsanitized user input via `Html.Raw`.

## Password Hashing

Use `IPasswordHasher<TUser>` from ASP.NET Core Identity:

```csharp
// Hash
var hashedPassword = _passwordHasher.HashPassword(user, password);

// Verify
var result = _passwordHasher.VerifyHashedPassword(user, storedHash, providedPassword);
if (result == PasswordVerificationResult.Failed)
{
    return Unauthorized();
}
```

**Do not** roll your own password hashing.

## Security Headers

Add security headers with middleware:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Xss-Protection", "1; mode=block");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    await next();
};
```

Or use `NWebsec` package for finer control.

## HSTS Preload

For production, enable HSTS:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(); // default is 30 days, configure with options
}
```

## Secret Scanning

- Add `appsettings.json` with non-sensitive defaults to repo
- Add `appsettings.*.json` with patterns like `appsettings.*.json` to `.gitignore` for environment-specific secrets
- Never commit `appsettings.Production.json` with actual connection strings

## Summary Checklist

✅ Use ASP.NET Core Identity or JWT for authentication  
✅ Use built-in password hashing, don't roll your own  
✅ Parameterize all SQL queries to avoid injection  
✅ Validate redirect URLs  
✅ Use DTOs to prevent mass assignment  
✅ Enable HTTPS/HSTS  
✅ Configure CORS with specific origins  
✅ Store secrets in secure configuration (not Git)  
✅ Add security headers  

## References

- [ASP.NET Core Security Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [OWASP Top Ten](https://owasp.org/www-project-top-ten/)
- [NIST Password Guidelines](https://pages.nist.gov/800-63-3/sp800-63b.html)
