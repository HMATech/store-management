# Store Management Backend

ASP.NET Core Minimal API backend for a store management app. It covers users, roles, brands, products (with prices, units and tags), purchases and sales.

For commit messages, follow [COMMIT_STYLE.md](COMMIT_STYLE.md).

## Branching

Always create a new branch before writing changes, and never work directly on `main`. Feature branches go under the `feature/` prefix, for example `feature/sales-endpoints`.

## Stack

- .NET 8 (`net8.0`), with nullable and implicit usings enabled in `Directory.Build.props`
- PostgreSQL through EF Core 9 (Npgsql) with snake_case naming
- FluentValidation, Scrutor, JWT bearer authentication, Swashbuckle
- Package versions are managed centrally: set them in `Directory.Packages.props` and leave `Version` off `PackageReference` in the `.csproj` files

## Commands

Run these from the repo root.

```sh
dotnet build Backend.sln
dotnet run --project src/Web.Api --launch-profile https   # https://localhost:7001/swagger

# Migrations (needs the dotnet-ef tool: dotnet tool install -g dotnet-ef)
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Web.Api --output-dir Database/Migrations
```

- Running the app needs PostgreSQL at the `MainDb` connection string in `src/Web.Api/appsettings.Development.json` (`localhost:5432`, database `store`).
- Pending migrations are applied at startup in Development only (`ApplyMigrations` in `Program.cs`).

## Testing

Always write tests for code changes, even though the repo has no test projects yet. If a change needs a test project that doesn't exist, create it and add it to `Backend.sln` as part of that change. Run the tests with `dotnet test Backend.sln` before committing.

- **Framework:** use xUnit. Add test package versions to `Directory.Packages.props` like any other package.
- **Location:** test projects go in `tests/`, next to `src/`, with one project per layer named `<Layer>.Tests`, such as `tests/Application.Tests` or `tests/Web.Api.Tests`.
- **API tests:** test endpoints with `WebApplicationFactory` against a real PostgreSQL database run through Testcontainers, because handlers use EF Core directly. These tests need Docker running.
- **Internal types:** handlers, validators and the generated `Program` class are internal. Expose them to the test project with an `InternalsVisibleTo` attribute in the project under test, written in source because `Directory.Build.props` turns off `GenerateAssemblyInfo`.

## Architecture

Clean Architecture with CQRS. Dependencies point inward:

| Project          | Responsibility                                       | References                       |
| ---------------- | ---------------------------------------------------- | -------------------------------- |
| `Shared`         | `Result`, `Error`, `ErrorType`, `IDateTimeProvider`  | none                             |
| `Domain`         | Entities and per-feature `*Errors` classes           | `Shared`                         |
| `Application`    | Commands, queries, handlers, validators, responses   | `Domain`, `Shared`               |
| `Infrastructure` | `ApplicationDbContext`, EF configurations, migrations, auth | `Domain`, `Application`   |
| `Web.Api`        | Endpoints, HTTP result mapping, exception handling   | `Infrastructure`                 |

There is no repository layer. Handlers query EF Core directly through `IApplicationDbContext`, which is why `Application` references EF Core.

## Adding to a feature

Folder and class names follow `<Feature>/<Action>`, for example `Brands/Create` or `Products/ProductTags/UpdateById`. `src/Application/Brands` and `src/Web.Api/Endpoints/Brands` are the simplest references.

1. **Domain** (`src/Domain/<Feature>/`): entities have private setters, a private constructor and a static `CreateNew` factory. Add errors to `<Feature>Errors`.
2. **Application** (`src/Application/<Feature>/<Action>/`):
   - `<Action><Feature>Command` or `Query`: a `public sealed record` implementing `ICommand`, `ICommand<TResponse>` or `IQuery<TResponse>`
   - `...Handler`: an `internal sealed class` using a primary constructor
   - `...Validator`: an `internal sealed class` extending `AbstractValidator<T>`
   - `...Response`: a `public sealed record`
3. **Infrastructure**: add new entities as a `DbSet` on both `IApplicationDbContext` and `ApplicationDbContext`, plus an `IEntityTypeConfiguration<T>` in `src/Infrastructure/<Feature>/<Feature>Configuration.cs`. Table names are singular snake_case (`ToTable("product_detail")`). Then add a migration.
4. **Web.Api** (`src/Web.Api/Endpoints/<Feature>/<Action>.cs`): an `internal sealed class` extending the feature's abstract endpoint base (such as `BrandEndpoint`), which sets the route group and tag. Define the request as a nested record, build the command or query, call the handler, and return `CustomHttpResults.TypedFrom(...)`. A new feature also needs a base class and a constant in `Tags.cs`.

Handlers, validators and endpoints are discovered automatically, so they don't need manual DI or route registration.

## Conventions

### Errors

- Handlers return `Result` or `Result<T>` instead of throwing for expected failures. Implicit conversions let them `return BrandErrors.NotFound(id);` or `return new CreateBrandResponse(brand.Id);`.
- Error types map to HTTP status in `CustomHttpResults`:

  | `ErrorType`             | Status |
  | ----------------------- | ------ |
  | `Validation`, `Problem` | 400    |
  | `NotFound`              | 404    |
  | `Conflict`              | 409    |
  | `System` and others     | 500    |

- Handlers wrap database work in `try`/`catch`, log the exception, and return the matching `ApplicationErrors` entry: `DBOperationError`, `OperationCancelledError` or `UnexpectedError`. Catch them in that order, and pass `nameof(<Handler>)` as the first argument.
- Error details for `System` errors are hidden from API responses, so put the useful information in the log.

### Validation

`ValidationDecorator` runs validators for commands only. Query handlers aren't decorated, so a validator written for a query won't run.

### Dates

- Use `IDateTimeProvider.UtcNow` rather than `DateTime.UtcNow`.
- `DateTime` properties are stored as UTC by a converter applied in `ConfigureDateTime`. Mark a property with `[NotUtc]` to opt out.

### Tags

Use `TagsHelper.CreateSynchronizedTagsAsync` to sync tags on an entity (see the Product, Purchase and Sale handlers).

### Authorization

`.RequireAuthorization()` is added per endpoint. Only the Users `DeleteById`, `GetUser`, `GetUserById` and `UpdateDetailById` endpoints have it so far, and the other endpoints are public. Swagger is set up to send a JWT bearer token.

### Migrations

Files in `src/Infrastructure/Database/Migrations` are generated. Don't edit them by hand except to fix a newly generated migration that hasn't been shared yet.

## Code style

### Naming

- Never prefix private fields with `_`. Private instance fields are camelCase (`private readonly ILogger<T> logger;`) and are assigned with `this.logger = logger;` when the names collide.
- Private constants and static fields are PascalCase (`private const int SaltSize = 16;`).
- Prefer primary constructors for injected dependencies, as handlers do, instead of declaring fields.

### Line length

Lines can be at most 120 columns. When wrapping:

- Continue parameter and argument lists on the next line with one extra indent.
- Break method chains before the `.`.
- Start the continuation line with the operator (`||`, `&&`, `?`, `:`).

### Member order

Arrange the members of a type in this order, with a blank line between groups:

1. Nested types (such as the request records in endpoint classes)
2. Constants and static fields
3. Instance fields
4. Events
5. Properties, with auto-properties before computed ones
6. Constructors
7. Instance methods
8. Static methods, including factories such as `CreateNew`

Within the method groups, put private helpers after the public methods.

```csharp
public sealed class Example
{
    public sealed record ExampleRequest(string Name);

    private const int MaxSize = 16;

    private readonly ILogger<Example> logger;

    public string Name { get; private set; } = string.Empty;
    public bool HasName => Name.Length > 0;

    private Example(ILogger<Example> logger)
    {
        this.logger = logger;
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public static Example CreateNew(ILogger<Example> logger) => new(logger);
}
```

## Known misnamed files

These file names don't match the types they contain. Use the type names when searching, and don't copy these names into new code:

| File                                                         | Contains                      |
| ------------------------------------------------------------ | ----------------------------- |
| `Application/Sales/DeleteById/DeletePurchaseByIdCommand.cs`  | `DeleteSaleByIdCommand`       |
| `Application/Sales/DeleteById/DeletePurchaseByIdCommandHandler.cs` | `DeleteSaleByIdCommandHandler` |
| `Application/Products/ProductTags/ProductUnitResponse.cs`    | `ProductTagResponse`          |
| `Application/Roles/UnassignUser/UnssignUserCommandHandler.cs` | `UnassignUserCommandHandler` |
| `Web.Api/Endpoints/Products/ProductPrices/ProductUnitEndpoint.cs` | `ProductPriceEndpoint`   |
