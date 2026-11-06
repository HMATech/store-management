using Application.Abstractions.Data;
using Application.Brands.Create;
using Application.Brands.DeleteById;
using Application.Brands.Get;
using Application.Brands.GetById;
using Application.Brands.UpdateById;
using Application.Common;
using Application.Products.Create;
using Application.Products.DeleteById;
using Application.Products.Get;
using Application.Products.GetById;
using Application.Products.ProductPrices.DeleteById;
using Application.Products.ProductPrices.Get;
using Application.Products.ProductPrices.GetById;
using Application.Products.ProductTags.Create;
using Application.Products.ProductTags.DeleteById;
using Application.Products.ProductTags.Get;
using Application.Products.ProductTags.GetById;
using Application.Products.ProductTags.UpdateById;
using Application.Products.ProductUnits.Create;
using Application.Products.ProductUnits.DeleteById;
using Application.Products.ProductUnits.Get;
using Application.Products.ProductUnits.GetById;
using Application.Products.ProductUnits.UpdateById;
using Application.Products.UpdateById;
using Application.Purchases.Create;
using Application.Purchases.DeleteById;
using Application.Purchases.Get;
using Application.Purchases.GetById;
using Application.Purchases.UpdateDetailsById;
using Application.Purchases.UpdateEntriesById;
using Application.Roles.AssignUser;
using Application.Roles.Create;
using Application.Roles.DeleteById;
using Application.Roles.Get;
using Application.Roles.GetById;
using Application.Roles.GetRolesByUser;
using Application.Roles.GetUsersByName;
using Application.Roles.UnassignUser;
using Application.Roles.UpdateById;
using Application.Sales.Create;
using Application.Sales.DeleteById;
using Application.Sales.Get;
using Application.Sales.GetById;
using Application.Sales.UpdateDetailsById;
using Application.Sales.UpdateEntriesById;
using Application.Tests.TestSupport;
using Application.Users.DeleteById;
using Application.Users.Get;
using Application.Users.GetById;
using Application.Users.Login;
using Application.Users.Register;
using Application.Users.UpdateDetailById;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Shared;
using PriceById = Application.Products.ProductPrices.UpdateById;
using ProductPriceById = Application.Products.UpdatePriceById;
using PurchaseCreateEntry = Application.Purchases.Create.ProductEntryCommand;
using PurchaseUpdateEntry = Application.Purchases.UpdateEntriesById.ProductEntryCommand;
using SaleCreateEntry = Application.Sales.Create.ProductEntryCommand;
using SaleUpdateEntry = Application.Sales.UpdateEntriesById.ProductEntryCommand;

namespace Application.Tests;

/// <summary>Checks that every handler turns database failures into the matching error result.</summary>
public class HandlerFailureTests
{
    private const string ForeignKeyViolation = "23503";

    private static readonly DateTime When = FixedClock.Now;

    private static readonly Dictionary<string, Func<IApplicationDbContext, SeededGraph, Task<Result>>> Commands = new()
    {
        [nameof(CreateBrandCommandHandler)] = async (db, g) =>
            await new CreateBrandCommandHandler(db, new FixedClock(), Log<CreateBrandCommandHandler>())
                .HandleAsync(new CreateBrandCommand("New"), default),
        [nameof(DeleteBrandByIdCommandHandler)] = async (db, g) =>
            await new DeleteBrandByIdCommandHandler(db, Log<DeleteBrandByIdCommandHandler>())
                .HandleAsync(new DeleteBrandByIdCommand(g.Brand.Id), default),
        [nameof(UpdateBrandCommandHandler)] = async (db, g) =>
            await new UpdateBrandCommandHandler(db, Log<UpdateBrandCommandHandler>())
                .HandleAsync(new UpdateBrandByIdCommand(g.Brand.Id, "New"), default),
        [nameof(CreateProductCommandHandler)] = async (db, g) =>
            await new CreateProductCommandHandler(db, new FixedClock(), Log<CreateProductCommandHandler>())
                .HandleAsync(new CreateProductCommand("New", null, 1f, g.Unit.Id, g.Brand.Id, 1m, []), default),
        [nameof(DeleteProductByIdCommandHandler)] = async (db, g) =>
            await new DeleteProductByIdCommandHandler(db, Log<DeleteProductByIdCommandHandler>())
                .HandleAsync(new DeleteProductByIdCommand(g.Product.Id), default),
        [nameof(UpdateProductByIdCommandHandler)] = async (db, g) =>
            await new UpdateProductByIdCommandHandler(db, new FixedClock(), Log<UpdateProductByIdCommandHandler>())
                .HandleAsync(new UpdateProductByIdCommand(g.Product.Id, "New", null, 1f, g.Unit.Id, 1m, []), default),
        [nameof(ProductPriceById.UpdateProductPriceByIdCommandHandler)] = async (db, g) =>
            await new ProductPriceById.UpdateProductPriceByIdCommandHandler(db, new FixedClock(),
                    Log<ProductPriceById.UpdateProductPriceByIdCommandHandler>())
                .HandleAsync(new ProductPriceById.UpdateProductPriceByIdCommand(g.Product.Id, 1m), default),
        [nameof(DeleteProductPriceByIdCommandHandler)] = async (db, g) =>
            await new DeleteProductPriceByIdCommandHandler(db, Log<DeleteProductPriceByIdCommandHandler>())
                .HandleAsync(new DeleteProductPriceByIdCommand(g.OldPrice.Id), default),
        [nameof(CreateProductTagCommandHandler)] = async (db, g) =>
            await new CreateProductTagCommandHandler(db, new FixedClock(), Log<CreateProductTagCommandHandler>())
                .HandleAsync(new CreateProductTagCommand("New"), default),
        [nameof(DeleteProductTagByIdCommandHandler)] = async (db, g) =>
            await new DeleteProductTagByIdCommandHandler(db, Log<DeleteProductTagByIdCommandHandler>())
                .HandleAsync(new DeleteProductTagByIdCommand(g.Tag.Id), default),
        [nameof(UpdateProductTagByIdCommandHandler)] = async (db, g) =>
            await new UpdateProductTagByIdCommandHandler(db, Log<UpdateProductTagByIdCommandHandler>())
                .HandleAsync(new UpdateProductTagByIdCommand(g.Tag.Id, "New"), default),
        [nameof(CreateProductUnitCommandHandler)] = async (db, g) =>
            await new CreateProductUnitCommandHandler(db, new FixedClock(), Log<CreateProductUnitCommandHandler>())
                .HandleAsync(new CreateProductUnitCommand("New"), default),
        [nameof(DeleteProductUnitByIdCommandHandler)] = async (db, g) =>
            await new DeleteProductUnitByIdCommandHandler(db, Log<DeleteProductUnitByIdCommandHandler>())
                .HandleAsync(new DeleteProductUnitByIdCommand(g.Unit.Id), default),
        [nameof(UpdateProductUnitByIdCommandHandler)] = async (db, g) =>
            await new UpdateProductUnitByIdCommandHandler(db, Log<UpdateProductUnitByIdCommandHandler>())
                .HandleAsync(new UpdateProductUnitByIdCommand(g.Unit.Id, "New"), default),
        [nameof(CreatePurchaseCommandHandler)] = async (db, g) =>
            await new CreatePurchaseCommandHandler(db, new FixedClock(), Log<CreatePurchaseCommandHandler>())
                .HandleAsync(new CreatePurchaseCommand("New", [], [new PurchaseCreateEntry(g.Product.Id, 1)], When),
                    default),
        [nameof(DeletePurchaseByIdCommandHandler)] = async (db, g) =>
            await new DeletePurchaseByIdCommandHandler(db, Log<DeletePurchaseByIdCommandHandler>())
                .HandleAsync(new DeletePurchaseByIdCommand(g.Purchase.Id), default),
        [nameof(UpdatePurchaseDetailsByIdCommandHandler)] = async (db, g) =>
            await new UpdatePurchaseDetailsByIdCommandHandler(db, new FixedClock(),
                    Log<UpdatePurchaseDetailsByIdCommandHandler>())
                .HandleAsync(new UpdatePurchaseDetailsByIdCommand(g.Purchase.Id, "New", [], When), default),
        [nameof(UpdatePurchaseEntriesByIdCommandHandler)] = async (db, g) =>
            await new UpdatePurchaseEntriesByIdCommandHandler(db, new FixedClock(),
                    Log<UpdatePurchaseEntriesByIdCommandHandler>())
                .HandleAsync(new UpdatePurchaseEntriesByIdCommand(g.Purchase.Id,
                    [new PurchaseUpdateEntry(g.PurchaseEntry.Id, g.Product.Id, 1)]), default),
        [nameof(CreateSaleCommandHandler)] = async (db, g) =>
            await new CreateSaleCommandHandler(db, new FixedClock(), Log<CreateSaleCommandHandler>())
                .HandleAsync(new CreateSaleCommand("New", [], [new SaleCreateEntry(g.Product.Id, 1)], When), default),
        [nameof(DeleteSaleByIdCommandHandler)] = async (db, g) =>
            await new DeleteSaleByIdCommandHandler(db, Log<DeleteSaleByIdCommandHandler>())
                .HandleAsync(new DeleteSaleByIdCommand(g.Sale.Id), default),
        [nameof(UpdateSaleDetailsByIdCommandHandler)] = async (db, g) =>
            await new UpdateSaleDetailsByIdCommandHandler(db, new FixedClock(),
                    Log<UpdateSaleDetailsByIdCommandHandler>())
                .HandleAsync(new UpdateSaleDetailsByIdCommand(g.Sale.Id, "New", [], When), default),
        [nameof(UpdateSaleEntriesByIdCommandHandler)] = async (db, g) =>
            await new UpdateSaleEntriesByIdCommandHandler(db, new FixedClock(),
                    Log<UpdateSaleEntriesByIdCommandHandler>())
                .HandleAsync(new UpdateSaleEntriesByIdCommand(g.Sale.Id,
                    [new SaleUpdateEntry(g.SaleEntry.Id, g.Product.Id, 1)]), default),
        [nameof(AssignUserCommandHandler)] = async (db, g) =>
            await new AssignUserCommandHandler(db, new FixedClock(), Log<AssignUserCommandHandler>())
                .HandleAsync(new AssignUserCommand(g.OtherRole.Id, g.User.Email), default),
        [nameof(CreateRoleCommandHandler)] = async (db, g) =>
            await new CreateRoleCommandHandler(db, new FixedClock(), Log<CreateRoleCommandHandler>())
                .HandleAsync(new CreateRoleCommand("New", null), default),
        [nameof(DeleteRoleByIdCommandHandler)] = async (db, g) =>
            await new DeleteRoleByIdCommandHandler(db, Log<DeleteRoleByIdCommandHandler>())
                .HandleAsync(new DeleteRoleByIdCommand(g.Role.Id), default),
        [nameof(UnassignUserCommandHandler)] = async (db, g) =>
            await new UnassignUserCommandHandler(db, Log<UnassignUserCommandHandler>())
                .HandleAsync(new UnassignUserCommand(g.Role.Id, g.User.Email), default),
        [nameof(UpdateRoleByIdCommandHandler)] = async (db, g) =>
            await new UpdateRoleByIdCommandHandler(db, Log<UpdateRoleByIdCommandHandler>())
                .HandleAsync(new UpdateRoleByIdCommand(g.Role.Id, "New", null), default),
        [nameof(DeleteUserByIdCommandHandler)] = async (db, g) =>
            await new DeleteUserByIdCommandHandler(db, Log<DeleteUserByIdCommandHandler>())
                .HandleAsync(new DeleteUserByIdCommand(g.User.Id), default),
        [nameof(RegisterUserCommandHandler)] = async (db, g) =>
            await new RegisterUserCommandHandler(new FakePasswordHasher(), db, new FixedClock(),
                    Log<RegisterUserCommandHandler>())
                .HandleAsync(new RegisterUserCommand("new@example.com", "Passw0rd!", "New", null), default),
        [nameof(UpdateUserDetailByIdCommandHandler)] = async (db, g) =>
            await new UpdateUserDetailByIdCommandHandler(db, Log<UpdateUserDetailByIdCommandHandler>())
                .HandleAsync(new UpdateUserDetailByIdCommand(g.User.Id, "New", null), default),
    };

    private static readonly Dictionary<string, Func<IApplicationDbContext, Task<Result>>> Queries = new()
    {
        [nameof(GetBrandQueryHandler)] = async db =>
            await new GetBrandQueryHandler(db, Log<GetBrandQueryHandler>())
                .HandleAsync(new GetBrandQuery(null, SortOrder.ASC), default),
        [nameof(GetBrandByIdQueryHandler)] = async db =>
            await new GetBrandByIdQueryHandler(db, Log<GetBrandByIdQueryHandler>())
                .HandleAsync(new GetBrandByIdQuery(1), default),
        [nameof(GetProductQueryHandler)] = async db =>
            await new GetProductQueryHandler(db, Log<GetProductQueryHandler>())
                .HandleAsync(new GetProductQuery(null, SortOrder.ASC), default),
        [nameof(GetProductByIdQueryHandler)] = async db =>
            await new GetProductByIdQueryHandler(db, Log<GetProductByIdQueryHandler>())
                .HandleAsync(new GetProductByIdQuery(Guid.NewGuid()), default),
        [nameof(GetProductPriceQueryHandler)] = async db =>
            await new GetProductPriceQueryHandler(db, Log<GetProductPriceQueryHandler>())
                .HandleAsync(new GetProductPriceQuery(null, SortOrder.ASC), default),
        [nameof(GetProductPriceByIdQueryHandler)] = async db =>
            await new GetProductPriceByIdQueryHandler(db, Log<GetProductPriceByIdQueryHandler>())
                .HandleAsync(new GetProductPriceByIdQuery(Guid.NewGuid()), default),
        [nameof(GetProductTagQueryHandler)] = async db =>
            await new GetProductTagQueryHandler(db, Log<GetProductTagQueryHandler>())
                .HandleAsync(new GetProductTagQuery(null, SortOrder.ASC), default),
        [nameof(GetProductTagByIdQueryHandler)] = async db =>
            await new GetProductTagByIdQueryHandler(db, Log<GetProductTagByIdQueryHandler>())
                .HandleAsync(new GetProductTagByIdQuery(1), default),
        [nameof(GetProductUnitQueryHandler)] = async db =>
            await new GetProductUnitQueryHandler(db, Log<GetProductUnitQueryHandler>())
                .HandleAsync(new GetProductUnitQuery(null, SortOrder.ASC), default),
        [nameof(GetProductUnitByIdQueryHandler)] = async db =>
            await new GetProductUnitByIdQueryHandler(db, Log<GetProductUnitByIdQueryHandler>())
                .HandleAsync(new GetProductUnitByIdQuery(1), default),
        [nameof(GetPurchaseQueryHandler)] = async db =>
            await new GetPurchaseQueryHandler(db, Log<GetPurchaseQueryHandler>())
                .HandleAsync(new GetPurchaseQuery(null, SortOrder.ASC, null, null, null, null), default),
        [nameof(GetPurchaseByIdQueryHandler)] = async db =>
            await new GetPurchaseByIdQueryHandler(db, Log<GetPurchaseByIdQueryHandler>())
                .HandleAsync(new GetPurchaseByIdQuery(Guid.NewGuid()), default),
        [nameof(GetSaleQueryHandler)] = async db =>
            await new GetSaleQueryHandler(db, Log<GetSaleQueryHandler>())
                .HandleAsync(new GetSaleQuery(null, SortOrder.ASC, null, null, null, null), default),
        [nameof(GetSaleByIdQueryHandler)] = async db =>
            await new GetSaleByIdQueryHandler(db, Log<GetSaleByIdQueryHandler>())
                .HandleAsync(new GetSaleByIdQuery(Guid.NewGuid()), default),
        [nameof(GetRoleQueryHandler)] = async db =>
            await new GetRoleQueryHandler(db, Log<GetRoleQuery>()).HandleAsync(new GetRoleQuery(null), default),
        [nameof(GetRoleByIdQueryHandler)] = async db =>
            await new GetRoleByIdQueryHandler(db, Log<GetRoleByIdQueryHandler>())
                .HandleAsync(new GetRoleByIdQuery(1), default),
        [nameof(GetRolesByUserQueryHandler)] = async db =>
            await new GetRolesByUserQueryHandler(db, Log<GetRolesByUserQueryHandler>())
                .HandleAsync(new GetRolesByUserQuery("a@b.com"), default),
        [nameof(GetUsersByNameQueryHandler)] = async db =>
            await new GetUsersByNameQueryHandler(db, Log<GetUsersByNameQueryHandler>())
                .HandleAsync(new GetUsersByNameQuery("admin"), default),
        [nameof(GetUserQueryHandler)] = async db =>
            await new GetUserQueryHandler(db, Log<GetUserByIdQueryHandler>())
                .HandleAsync(new GetUserQuery(null), default),
        [nameof(GetUserByIdQueryHandler)] = async db =>
            await new GetUserByIdQueryHandler(db, Log<GetUserByIdQueryHandler>())
                .HandleAsync(new GetUserByIdQuery(Guid.NewGuid()), default),
    };

    private readonly TestDatabase database = new();

    public static TheoryData<string> CommandNames => new(Commands.Keys);
    public static TheoryData<string> QueryNames => new(Queries.Keys);

    [Theory]
    [MemberData(nameof(CommandNames))]
    public async Task Command_WhenSaveThrowsDbUpdateException_ReturnsDbOperationError(string handler)
    {
        var result = await RunCommandAsync(handler, new DbUpdateException("Database failure"));

        ResultAssert.Failed(result, $"{handler}.{ApplicationErrorDefaults.DBOperationErrorTitle}", ErrorType.System);
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public async Task Command_WhenSaveIsCancelled_ReturnsOperationError(string handler)
    {
        var result = await RunCommandAsync(handler, new OperationCanceledException());

        ResultAssert.Failed(result, $"{handler}.{ApplicationErrorDefaults.OperationCancelledErrorTitle}",
            ErrorType.System);
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public async Task Command_WhenSaveThrowsUnexpectedException_ReturnsUnexpectedError(string handler)
    {
        var result = await RunCommandAsync(handler, new InvalidOperationException("Boom"));

        ResultAssert.Failed(result, $"{handler}.{ApplicationErrorDefaults.UnexpectedErrorTitle}", ErrorType.System);
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public async Task Command_WhenSaveFails_DoesNotPersistChanges(string handler)
    {
        var graph = await SeededGraph.CreateAsync(database);
        var counts = await CountRowsAsync();
        await using (var context = database.CreateContext())
        {
            await Commands[handler](new SaveFailingDbContext(context, new DbUpdateException("x")), graph);
        }

        Assert.Equal(counts, await CountRowsAsync());
    }

    [Theory]
    [MemberData(nameof(QueryNames))]
    public async Task Query_WhenCancelled_ReturnsOperationError(string handler)
    {
        var result = await Queries[handler](new ThrowingDbContext(new OperationCanceledException()));

        ResultAssert.Failed(result, $"{handler}.{ApplicationErrorDefaults.OperationCancelledErrorTitle}",
            ErrorType.System);
    }

    [Theory]
    [MemberData(nameof(QueryNames))]
    public async Task Query_WhenDatabaseThrows_ReturnsUnexpectedError(string handler)
    {
        var result = await Queries[handler](new ThrowingDbContext(new InvalidOperationException("Boom")));

        ResultAssert.Failed(result, $"{handler}.{ApplicationErrorDefaults.UnexpectedErrorTitle}", ErrorType.System);
    }

    [Fact]
    public void FailureTables_CoverEveryHandler()
    {
        var handlerNames = typeof(CreateBrandCommand).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, DeclaringType: null } && t.Name.EndsWith("Handler"))
            .Select(t => t.Name)
            .ToHashSet();

        // UpdateProductPriceByIdCommandHandler exists in two namespaces; PriceUpdateById_WhenSaveFails_ReturnsErrors
        // covers the second one.
        // LoginUserCommandHandler has its own tests below because it never saves.
        handlerNames.Remove(nameof(LoginUserCommandHandler));

        Assert.Empty(handlerNames.Except(Commands.Keys).Except(Queries.Keys));
    }

    [Theory]
    [InlineData(nameof(DeleteBrandByIdCommandHandler), "Brands.InUse")]
    [InlineData(nameof(DeleteProductByIdCommandHandler), "Products.InUse")]
    [InlineData(nameof(DeleteProductPriceByIdCommandHandler), "Products.PriceInUse")]
    [InlineData(nameof(DeleteProductUnitByIdCommandHandler), "Products.UnitIsStillReferenced")]
    public async Task Delete_WhenForeignKeyIsViolated_ReturnsConflict(string handler, string expectedCode)
    {
        var exception = new DbUpdateException("x", new PostgresException("m", "ERROR", "ERROR", ForeignKeyViolation));

        var result = await RunCommandAsync(handler, exception);

        ResultAssert.Failed(result, expectedCode, ErrorType.Conflict);
    }

    [Theory]
    [InlineData(nameof(DeleteBrandByIdCommandHandler))]
    [InlineData(nameof(DeleteProductByIdCommandHandler))]
    [InlineData(nameof(DeleteProductPriceByIdCommandHandler))]
    [InlineData(nameof(DeleteProductUnitByIdCommandHandler))]
    public async Task Delete_WhenAnotherPostgresErrorOccurs_ReturnsDbOperationError(string handler)
    {
        var exception = new DbUpdateException("x", new PostgresException("m", "ERROR", "ERROR", "23505"));

        var result = await RunCommandAsync(handler, exception);

        ResultAssert.Failed(result, $"{handler}.{ApplicationErrorDefaults.DBOperationErrorTitle}", ErrorType.System);
    }

    [Fact]
    public async Task PriceUpdateById_WhenSaveFails_ReturnsErrors()
    {
        var graph = await SeededGraph.CreateAsync(database);
        const string Handler = nameof(PriceById.UpdateProductPriceByIdCommandHandler);

        async Task<Result> RunAsync(Exception exception)
        {
            await using var context = database.CreateContext();
            var handler = new PriceById.UpdateProductPriceByIdCommandHandler(
                new SaveFailingDbContext(context, exception), Log<PriceById.UpdateProductPriceByIdCommandHandler>());
            return await handler.HandleAsync(
                new PriceById.UpdateProductPriceByIdCommand(graph.OldPrice.Id, 1m), default);
        }

        ResultAssert.Failed(await RunAsync(new DbUpdateException("x")), $"{Handler}.DBOperationError",
            ErrorType.System);
        ResultAssert.Failed(await RunAsync(new OperationCanceledException()), $"{Handler}.OperationError",
            ErrorType.System);
        ResultAssert.Failed(await RunAsync(new InvalidOperationException()), $"{Handler}.UnexpectedError",
            ErrorType.System);
    }

    [Fact]
    public async Task Login_WhenCancelled_ReturnsOperationError()
    {
        var result = await LoginAsync(new OperationCanceledException());

        ResultAssert.Failed(result, "LoginUserCommandHandler.OperationError", ErrorType.System);
    }

    [Fact]
    public async Task Login_WhenDatabaseThrows_ReturnsUnexpectedError()
    {
        var result = await LoginAsync(new InvalidOperationException("Boom"));

        ResultAssert.Failed(result, "LoginUserCommandHandler.UnexpectedError", ErrorType.System);
    }

    private static NullLogger<T> Log<T>()
    {
        return NullLogger<T>.Instance;
    }

    private static async Task<Result> LoginAsync(Exception exception)
    {
        var handler = new LoginUserCommandHandler(new FakePasswordHasher(), new ThrowingDbContext(exception),
            new FakeTokenProvider(), Log<LoginUserCommandHandler>());
        return await handler.HandleAsync(new LoginUserCommand("a@b.com", "x"), default);
    }

    private async Task<Result> RunCommandAsync(string handler, Exception exception)
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        return await Commands[handler](new SaveFailingDbContext(context, exception), graph);
    }

    private async Task<int[]> CountRowsAsync()
    {
        await using var context = database.CreateContext();
        return
        [
            await context.Brands.CountAsync(), await context.Products.CountAsync(),
            await context.ProductPrices.CountAsync(), await context.ProductTags.CountAsync(),
            await context.ProductUnits.CountAsync(), await context.Purchases.CountAsync(),
            await context.PurchaseProductEntries.CountAsync(), await context.Sales.CountAsync(),
            await context.SaleProductEntries.CountAsync(), await context.Roles.CountAsync(),
            await context.UserRoles.CountAsync(), await context.Users.CountAsync(),
        ];
    }
}
