# Store Management Backend

ASP.NET Core Minimal API backend for a store management app. It covers users, roles, brands, products (with prices, units and tags), purchases and sales.

For commit messages, follow [COMMIT_STYLE.md](COMMIT_STYLE.md).

This file keeps evolving with the project's needs. When a change makes it wrong or incomplete, or a new convention is agreed, update it in the same change.

## Working standards

Apply these to all work, however small the change.

### Robustness

- Handle failure paths as carefully as the success path: invalid input, missing data, conflicts, cancellation and database errors.
- Validate input at the boundary, and return an expected failure as a `Result` error with the right status. A client mistake should never end in a 500.
- Check related code when you fix a bug. The same mistake often exists in a sibling handler, such as the purchase and sale versions of a feature.
- Cover failure paths with negative tests, and show a bug fix works with a test that fails without it.
- Run the build and tests before calling work done, and report any failure or skipped step.

### Clean code

- Keep methods and classes focused, with names that say what they do.
- Follow the existing patterns in this file and the surrounding code rather than introducing new ones.
- Reuse existing helpers instead of duplicating logic, and remove dead code, including anything your change leaves unused.
- Keep each change to its purpose. Raise unrelated problems separately instead of mixing them in.
- Follow the rules in [Code style](#code-style).

## Branching

Always create a new branch before writing changes, and never work directly on `main`. Feature branches go under the `feature/` prefix, for example `feature/sales-endpoints`. Bug fix branches go under the `fix/` prefix, for example `fix/known-bugs`.

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

# Tests
dotnet test Backend.sln                                       # All tests, needs Docker running
dotnet test Backend.sln --filter "Category!=Integration"      # Unit tests only, no Docker needed
dotnet test Backend.sln --filter "Category=Integration"       # Integration tests only

# Migrations (needs the dotnet-ef tool: dotnet tool install -g dotnet-ef)
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Web.Api --output-dir Database/Migrations
```

- Running the app needs PostgreSQL at the `MainDb` connection string in `src/Web.Api/appsettings.Development.json` (`localhost:5432`, database `store`).
- Pending migrations are applied at startup in Development only (`ApplyMigrations` in `Program.cs`).

## Testing

Always write tests for code changes, including negative cases. If a change needs a test project that doesn't exist, create it and add it to `Backend.sln` as part of that change. Run the tests with `dotnet test Backend.sln` before committing.

- **Framework:** use xUnit. Add test package versions to `Directory.Packages.props` like any other package.
- **Location:** test projects go in `tests/`, next to `src/`, with one project per layer named `<Layer>.Tests`, such as `tests/Application.Tests` or `tests/Web.Api.Tests`.
- **API tests:** test endpoints with `WebApplicationFactory` against a real PostgreSQL database run through Testcontainers, because handlers use EF Core directly. These tests need Docker running. Mark them with `[Trait("Category", "Integration")]` so they can be filtered, and put them in the project's `Integration` folder.
- **Signed-in clients:** `CreateClient()` on the test hosts sends a valid bearer token, because every endpoint except `Login` requires one. Use `CreateAnonymousClient()` to test what an unauthenticated caller gets.
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

`ValidationDecorator` runs validators for both commands and queries, so adding an `AbstractValidator<T>` for either is enough.

### Dates

- Use `IDateTimeProvider.UtcNow` rather than `DateTime.UtcNow`.
- `DateTime` properties are stored as UTC by a converter applied in `ConfigureDateTime`. Mark a property with `[NotUtc]` to opt out.

### Tags

Use `TagsHelper.CreateSynchronizedTagsAsync` to sync tags on an entity (see the Product, Purchase and Sale handlers).

### Purchase and sale entries

An entry line is a product at a price, and it keeps that price when the product's price changes later. Repeated products in a request add together. On `PUT /{id}/entries`, a new entry joins a kept line only if that line is at the product's current price; otherwise it becomes a new line at the current price, so a product can have more than one line.

### Authorization

- A fallback policy in `AddAuthorizationInternal` makes every endpoint require a signed-in user, so new endpoints are protected without extra code. Don't add `.RequireAuthorization()` to endpoints.
- An endpoint that must stay public opts out with `.AllowAnonymous()`. Only Users `Login` does so far.
- Roles aren't checked yet. Any signed-in user can call any endpoint.
- `Register` needs a signed-in user, so the first account comes from the `InitialUser` configuration section (`Email`, `Password`, `FirstName`, optional `LastName`). On startup, `CreateInitialUserAsync` creates that user when the database has no users, and startup fails if the configured user is invalid. `appsettings.Development.json` sets a local one (`admin@store.local` / `Admin123!`).
- Swagger is set up to send a JWT bearer token.

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
