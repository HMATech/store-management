# Store Management

A store management app: an ASP.NET Core Minimal API backend and a React frontend. It covers users, roles, brands, products (with prices, units and tags), purchases and sales.

## Repository layout

| Path                 | Holds                                                                  |
| -------------------- | ---------------------------------------------------------------------- |
| `backend/`           | The API: `src/`, `tests/`, `Backend.sln`, `Directory.*.props`, `Dockerfile` |
| `frontend/`          | The React app: a Vite single-page PWA                                  |
| `docker-compose.yml` | Runs the deployed app, with its `.env.example`                         |

The sections from [Stack](#stack) to [Known misnamed files](#known-misnamed-files) are about the backend, and their paths are relative to `backend/` unless they say otherwise. [Frontend](#frontend) covers `frontend/`.

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

Always create a new branch before writing changes, and never work directly on `main`. Feature branches go under the `feature/` prefix and bug fix branches under the `fix/` prefix.

Name the branch after the work in a few kebab-case words, so the name alone says what changes. A single word such as `feature/stock` is too vague.

- Good: `feature/purchase-entries-record-buying-price`, `feature/daily-sales-and-purchase-totals`, `fix/sale-entry-merge-ignores-old-price`
- Too vague: `feature/stock`, `feature/purchases`, `fix/bugs`

## Stack

- .NET 8 (`net8.0`), with nullable and implicit usings enabled in `Directory.Build.props`
- PostgreSQL through EF Core 9 (Npgsql) with snake_case naming
- FluentValidation, Scrutor, JWT bearer authentication, Swashbuckle
- Package versions are managed centrally: set them in `Directory.Packages.props` and leave `Version` off `PackageReference` in the `.csproj` files

## Commands

Run these from the `backend` folder.

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
- Pending migrations are applied at startup (`ApplyMigrations` in `Program.cs`) in Development, and in other environments when `Database:ApplyMigrationsOnStartup` is `true`.

### Docker

Run these from the repo root. The compose file builds the API image from `backend/`.

```sh
cp .env.example .env            # Then fill in the secrets
docker compose up -d --build    # API on http://localhost:8080, with PostgreSQL in a volume
```

- `docker-compose.yml` runs the API in Production with startup migrations on, and maps the `.env` values to configuration keys such as `Jwt__Secret` and `InitialUser__Email`. A new configuration value the deployment needs goes in both files.
- The container serves plain HTTP on port 8080, with TLS expected at a reverse proxy. `ASPNETCORE_FORWARDEDHEADERS_ENABLED` makes the API trust the proxy's `X-Forwarded-Proto`, so `Location` URLs use `https`.

## Testing

Always write tests for code changes, including negative cases. If a change needs a test project that doesn't exist, create it and add it to `Backend.sln` as part of that change. Run the tests with `dotnet test Backend.sln` from `backend/` before committing.

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

- **Sales:** an entry line is a product at a selling price line (`PriceId`), and it keeps that price when the product's price changes later. Repeated products in a request add together. On `PUT /{id}/entries`, a new entry joins a kept line only if that line is at the product's current price; otherwise it becomes a new line at the current price, so a product can have more than one line.
- **Purchases:** an entry line is a product at a unit cost, the price actually paid to the supplier (`UnitCost`). It is entered with each entry and isn't linked to the selling price, so purchases never block deleting a price line. Repeats of the same product and cost add together, and the same product at two costs makes two lines. On `PUT /{id}/entries`, a kept line can have its quantity and unit cost corrected, a new entry joins a kept line with the same product and cost, and listing one line twice with different costs is a validation error.
- Purchase responses report `unitCost` and `totalCost`; sale responses report `priceValue` and `totalPrice`.
- A sale's `title` is optional: the endpoints store a missing title as an empty string, and the validators only reject `null`. A purchase's title is required, because it records where the goods came from.

### Stock

- Stock isn't stored. It's worked out on each read as purchased minus sold plus adjusted, by `StockQueries.SelectStock`. Use that helper for anything that shows stock, rather than summing entries again.
- A `StockAdjustment` records any other change, such as spoilage or a correction after counting. A positive quantity adds stock and a negative one removes it, and zero is rejected.
- Stock can go negative, for example when a sale is recorded before its purchase. That's allowed so the records match what happened.
- Purchases and sales count whole products. A product sold both singly and by the carton is two products.
- Quantities are summed as `long`, so the totals in `ProductStockResponse` can't overflow.
- Each product has a `StockDefinition`: the rules for judging its stock, set when the product is added. `MinimumQuantity` is the first rule and defaults to `StockDefinition.DefaultMinimumQuantity` (5) when left empty. Add future rules as properties on `StockDefinition` and its command and response, not as loose product fields. On `PUT /products/{id}`, leaving `stockDefinition` out keeps the current one.
- Stock responses include `minimumQuantity` and a `status`: `Negative` below zero, `Low` from zero up to the minimum, `Normal` above it. `StockQueries.StatusOf` decides it, and `StockQueries.WhereStockStatus` applies the same rules in SQL. Keep the two in step, and use `WhereStockStatus` for any status filter.
- Product responses (`GET /products` and `GET /products/{id}`) include `stock` (`quantity` and `status`), so a product list doesn't need a second call. `GET /products?stockStatus=` filters by status. The list pages the products first, then loads the stock of that page in one query.
- The minimum's column has a database default of 5 for existing rows and a sentinel of `int.MinValue`, so EF always writes the value and a minimum of 0 isn't replaced by the default.

### Reports

- `GET /reports/daily-totals?from=&to=&timeZone=` returns sales and purchase totals and counts for every day in the range, including days with nothing, plus totals for the whole range. Sales are valued at their selling price and purchases at their unit cost.
- Occurrence times are stored in UTC, so days are counted in the `timeZone` the caller passes (an IANA id such as `Asia/Jakarta`, `UTC` by default). The frontend should pass the store's time zone.
- Each transaction's total is summed in SQL and only the grouping by local day happens in memory. A range covers at most 366 days.
- The `occurenceFrom`/`occurenceTo` filters on `/purchases` and `/sales` still compare UTC dates.

### Transaction history

- `GET /transactions` lists sales and purchases together, newest first by default, as `TransactionSummaryResponse` rows: `kind` (`Sale` or `Purchase`), title, time, `lineCount`, `quantity` and `total` (selling price for a sale, cost for a purchase).
- It filters by `kind` and by `from`/`to` days in `timeZone`, like the daily report, and pages with `page`/`pageSize`.
- Both sides project to the same row type and are combined with `Concat` in SQL, so sorting, counting and paging cover both. Rows are ordered by time, then id, so pages don't overlap.

### Authorization

- A fallback policy in `AddAuthorizationInternal` makes every endpoint require a signed-in user, so new endpoints are protected without extra code. Don't add `.RequireAuthorization()` to endpoints.
- An endpoint that must stay public opts out with `.AllowAnonymous()`. Only Users `Login` does so far.
- Roles aren't checked yet. Any signed-in user can call any endpoint.
- `Register` needs a signed-in user, so the first account comes from the `InitialUser` configuration section (`Email`, `Password`, `FirstName`, optional `LastName`). On startup, `CreateInitialUserAsync` creates that user when the database has no users, and startup fails if the configured user is invalid. `appsettings.Development.json` sets a local one (`admin@store.local` / `Admin123!`).
- Swagger is set up to send a JWT bearer token.

### CORS

Browsers can call the API only from origins listed in `Cors:AllowedOrigins`, and none are allowed when the list is empty. `appsettings.Development.json` allows `http://localhost:3000` for a local frontend. The policy exposes the `Location` header so a frontend can read the id of a created resource.

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

## Frontend

A React single-page app in `frontend/`, installable as a PWA. Its users aren't tech-savvy, so the UI is in everyday Bahasa Indonesia. Code, comments and tests are in English.

### Stack

- React 19 and TypeScript on Vite, with React Router and TanStack Query for anything that calls the API
- Tailwind CSS 4, with `cva` and `clsx` for component variants
- Base UI (`@base-ui/react`) for dialogs and other widgets that need focus handling, styled with the design's tokens
- Lucide icons (`lucide-react`), the icon set the design uses, and the Inter font bundled through `@fontsource-variable/inter` so it works offline
- `vite-plugin-pwa` for the manifest and service worker
- Vitest with Testing Library (jsdom), and oxlint

### Commands

Run these from the `frontend` folder.

```sh
npm install
npm run dev      # http://localhost:3000, the origin the API allows in development
npm test         # Vitest, no backend needed
npm run lint
npm run build    # Type-checks, then builds dist/ with the service worker
```

- The API's base URL comes from `VITE_API_URL`. Never hard-code it, because a packaged app calls the API from another origin.
- In development it is `/api`, which the dev server passes on to `https://localhost:7001` (the proxy in `vite.config.ts`). The browser then needn't trust the API's development certificate, and the calls are same-origin.
- A built app calls the API directly, so its origin must be in the backend's `Cors:AllowedOrigins`. The dev server stays on port 3000, the origin allowed in development.

### Structure

| Path               | Holds                                                                    |
| ------------------ | ------------------------------------------------------------------------ |
| `src/lib/`         | `api.ts` (the only place that calls `fetch`), `session.ts`, `format.ts`, `config.ts` |
| `src/features/`    | One folder per feature, such as `auth/`, with its API calls and hooks    |
| `src/components/`  | Shared UI components, with the app's frame in `shell/`                   |
| `src/pages/`       | One component per screen                                                 |
| `src/styles/`      | `tokens.css`, the design tokens                                          |
| `src/test/`        | Test setup and fixtures                                                  |

### Design system

The design lives in the Loom (Penpot) file "Dashboard", and the frontend follows it rather than a library's defaults.

- **Tokens:** `src/styles/tokens.css` is a copy of Loom's token sets under the same names (`color.bg.canvas` is `--color-bg-canvas`). Components use only the semantic tokens, through Tailwind utilities such as `bg-bg-canvas`. Never write a raw colour in a component. When a token changes, change it in Loom first, then here, and update the sync date in the file.
- **Spacing is in pixels.** Loom names a spacing step by its pixels, so `p-16` is 16px and `gap-8` is 8px. Use only Loom's steps: 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 28, 32, 36, 40.
- **Text sizes** map as `text-xs` 14, `text-sm` 15, `text-base` 16 (Loom's `md`), `text-lg` 17, `text-xl` 18, `text-2xl` 20, `text-3xl` 22, `text-4xl` 26, `text-5xl` 28, all at a 1.3 line height. Border widths are `border-thin`, `border-medium` and `border-thick`.
- **Screens:** before building a screen, read its board in Loom for the layout, tokens and wording, and compare a screenshot of the result with the board.
- **Dark mode** changes token values, not names. It follows the system, and `data-theme="light"` or `"dark"` on `<html>` forces one.
- **Components** mirror Loom's components and variant names: `Button` has `kind`, as Loom's Button has Kind. Define variants with `cva`. Icons are decorative next to their text label, so mark them `aria-hidden`.
- **Layout** is flex or grid with `min-height` tokens. Fixed heights are only for icons.
- **For these users:** body text is 16px (14px at the smallest), touch targets are at least 44px and main buttons 56px, every icon has a text label, and a screen has one primary action.

### Conventions

- **API calls** go through `apiRequest` in `src/lib/api.ts`. It adds the bearer token, throws `ApiError` (status 0 means no response arrived), and ends the session on a 401.
- **The session token** is read and written only through `src/lib/session.ts`, so its storage can change in one place.
- **Screens:** a signed-in screen is a route inside `AppShell` in `App.tsx`, wrapped in `Screen`, which gives it the top bar. `AppShell` shows the sidebar on wide screens (`lg` and up) and the bottom navigation on phones; add a main destination to `shell/navigation.ts`. Routes are in Indonesian, like the UI (`/jual`, `/produk`).
- **Task screens:** a screen entered to do one job, such as Catat Penjualan, gives `Screen` a `backTo` and sits under `<AppShell bottomNavigation={false}>`. On phones it then has a back button and the whole screen, with its main action pinned to the bottom; wide screens keep the sidebar.
- **Saving:** write with `useMutation`, disable the action while it runs, and on success call `queryClient.invalidateQueries()` so stock, totals and lists are read again. Keep what the user entered when saving fails, and say plainly that it wasn't saved.
- **Confirmations** that must not be missed, such as "Penjualan tersimpan", are a Base UI `AlertDialog` that stays until one of its actions is chosen, not a toast.
- **Product lists** load through `useProductPages` in `features/products`, a page at a time with `LoadMoreButton`. Use it wherever products are listed, as the sale and product screens do.
- **Filters** that narrow a list live in the address (`/produk?stok=minus`), so a link can open the list already filtered and going back keeps the choice. An unknown value means no filter.
- **An empty screen** uses `EmptyState` with the action that fills it; an empty search or filter result uses `Empty` and says what was looked for.
- **Search boxes** wait for a pause in typing (`useDebouncedValue`) and keep the previous results on screen while the new ones load.
- **Fetching:** read data with `useQuery`, passing the `signal` on to `apiRequest`. Each part of a screen fetches for itself and shows `Loading`, `LoadFailed` (with its retry) or `Empty` from `components/LoadState.tsx`, so one failed request doesn't blank the screen. Fetched data is cleared when the session ends.
- **Formatting:** amounts, dates and times go through `src/lib/format.ts` (`Rp 1.245.000`, `Minggu, 8 November 2026`, `Pukul 10.42`). Days are counted in the device's time zone, which is also the `timeZone` sent to the API.
- **Messages** tell the user what to do next in plain words, and never show an error code or the API's English text.
- **Mobile:** gate hover styles behind hover-capable devices (Tailwind's `hover:` does), give controls an `:active` state, keep inputs at 16px or more, use `dvh` for full-height layouts, and never disable zoom.
- **Motion** is short and subtle, and off under `prefers-reduced-motion`.

### Testing

- Test files sit next to the code as `*.test.ts(x)`.
- Test through what the user sees: find elements by role and label, and assert on the Indonesian text.
- Render the whole app at a route with `renderApp` from `src/test/render.tsx`, and stub the API per path with `stubApi` from `src/test/fixtures.ts`, which also builds tokens and responses.
- `Rp` amounts contain a no-break space, which Testing Library reads as a space. Compare against `formatRupiah` with that replaced, as `HomePage.test.tsx` does.
- Cover the failure paths: invalid input, a refused request, no connection, and a malformed response.
