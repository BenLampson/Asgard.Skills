---
name: dotnet-unit-testing
description: .NET unit testing standards skill. Use when creating, updating, reviewing, or migrating .NET/C# unit tests, test projects, test package references, test fixtures, assertions, mocks, test data builders, or CI test commands. Requires xUnit v3 for new and updated tests; use when avoiding or replacing xUnit 2.x packages and APIs.
---

# .NET Unit Testing

## Scope

Use this skill for .NET unit test work in C# projects. The core rule is mandatory:

- New .NET unit tests must use **xUnit v3**, not xUnit 2.x.
- When touching an existing xUnit 2.x test project, migrate the project/package references to xUnit v3 as part of the change unless the user explicitly asks for a no-migration patch.
- Do not add `xunit` or `xunit.runner.visualstudio` 2.x package versions copied from old xUnit 2.x templates.

## Package Rules

Prefer central package management when the repository already uses `Directory.Packages.props`; otherwise use local `PackageReference` versions consistent with the repo.

Typical xUnit v3 test package set:

```xml
<PackageReference Include="xunit.v3" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

If the repository pins package versions locally, choose current stable xUnit v3 package versions and keep all xUnit packages on the same major line.

Do not introduce the xUnit 2.x framework package for new work, and do not pin the Visual Studio runner to 2.x:

```xml
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.x.x" />
```

## Project Setup Checklist

When adding or reviewing a test project:

1. Use the repo's target framework unless there is a clear reason to differ.
2. Enable nullable reference types if the repo does.
3. Set `IsPackable` to `false` for test projects.
4. Reference the production project directly with `ProjectReference`.
5. Keep test-only dependencies in the test project, not production projects.
6. Make the test project discoverable by existing solution files and CI commands.

Minimal project shape:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\src\Example\Example.csproj" />
  </ItemGroup>
</Project>
```

## Test Style

Write small, deterministic tests around externally visible behavior. Prefer arranging real domain objects over over-mocking internals.

- Use `[Fact]` for single examples.
- Use `[Theory]` with `[InlineData]`, `[MemberData]`, or `[ClassData]` for input matrices.
- Use `async Task`, not `async void`.
- Avoid test order dependencies.
- Avoid real time, real network, real filesystem, and real database access in unit tests.
- Use clear test names that describe behavior, for example `CreateAsync_ReturnsId_WhenInputIsValid`.
- Keep assertions focused; one behavior can have multiple assertions, but unrelated behaviors need separate tests.

Example:

```csharp
using FluentAssertions;

namespace Example.Tests;

public class PriceCalculatorTests
{
    [Theory]
    [InlineData(100, 0.10, 90)]
    [InlineData(50, 0, 50)]
    public void ApplyDiscount_ReturnsDiscountedPrice(decimal price, decimal rate, decimal expected)
    {
        var calculator = new PriceCalculator();

        var actual = calculator.ApplyDiscount(price, rate);

        actual.Should().Be(expected);
    }
}
```

## Assertions And Mocks

Prefer the assertion and mock libraries already used in the repository.

- If FluentAssertions is already present, continue using it for readability.
- If the repo uses raw xUnit assertions, keep the local style unless changing it materially improves clarity.
- Use mocks only at process boundaries or expensive collaborators. Do not mock simple value objects, entities, or pure functions.
- Prefer hand-written fakes for simple dependencies when they make the test easier to read than a mock setup.

## Migration From xUnit 2.x

When migrating a touched test project:

1. Replace `xunit` with `xunit.v3`.
2. Update `xunit.runner.visualstudio` to a 3.x version or later; the runner package name stays the same.
3. Keep `Microsoft.NET.Test.Sdk`, updating only if necessary for discovery.
4. Run `dotnet restore` and `dotnet test`.
5. Fix compile errors caused by API changes instead of downgrading packages.

If migration affects many projects, make the change consistently across all test projects in the solution.

## Verification

Before claiming the work is done:

```powershell
dotnet restore
dotnet test
```

If the repository has a narrower solution, project, or CI script, run the smallest command that verifies the changed tests and mention exactly what was run.
