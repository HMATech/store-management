using Application.Brands.Create;
using Application.Brands.UpdateById;
using Application.Products.Create;
using Application.Products.ProductTags.Create;
using Application.Products.ProductTags.UpdateById;
using Application.Products.ProductUnits.Create;
using Application.Products.ProductUnits.UpdateById;
using Application.Products.UpdateById;
using Application.Purchases.Create;
using Application.Purchases.UpdateDetailsById;
using Application.Purchases.UpdateEntriesById;
using Application.Roles.AssignUser;
using Application.Roles.Create;
using Application.Roles.UnassignUser;
using Application.Roles.UpdateById;
using Application.Sales.Create;
using Application.Sales.UpdateDetailsById;
using Application.Sales.UpdateEntriesById;
using Application.Users.Register;
using Application.Users.UpdateDetailById;
using Application.Users.Login;
using FluentValidation;
using LoginValidator = Application.Users.Login.RegisterUserCommandValidator;
using PriceUpdate = Application.Products.ProductPrices.UpdateById;
using ProductPriceUpdate = Application.Products.UpdatePriceById;
using PurchaseCreateEntry = Application.Purchases.Create.ProductEntryCommand;
using PurchaseUpdateEntry = Application.Purchases.UpdateEntriesById.ProductEntryCommand;
using RegisterValidator = Application.Users.Register.RegisterUserCommandValidator;
using SaleCreateEntry = Application.Sales.Create.ProductEntryCommand;
using SaleUpdateEntry = Application.Sales.UpdateEntriesById.ProductEntryCommand;

namespace Application.Tests;

public class ValidatorTests
{
    private static readonly DateTime When = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid SomeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData("Acme", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void NameOnlyCommands_RequireName(string? name, bool expected)
    {
        Assert.Equal(expected, new CreateBrandCommandValidator().Validate(new CreateBrandCommand(name!)).IsValid);
        Assert.Equal(expected,
            new UpdateBrandByIdCommandValidator().Validate(new UpdateBrandByIdCommand(1, name!)).IsValid);
        Assert.Equal(expected,
            new CreateProductTagCommandValidator().Validate(new CreateProductTagCommand(name!)).IsValid);
        Assert.Equal(expected,
            new UpdateProductTagByIdCommandValidator().Validate(new UpdateProductTagByIdCommand(1, name!)).IsValid);
        Assert.Equal(expected,
            new CreateProductUnitCommandValidator().Validate(new CreateProductUnitCommand(name!)).IsValid);
        Assert.Equal(expected,
            new UpdateProductUnitByIdCommandValidator().Validate(new UpdateProductUnitByIdCommand(1, name!)).IsValid);
    }

    [Theory]
    [InlineData("Abc", true)]
    [InlineData("Ab", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void RoleCommands_RequireNameOfAtLeastThreeCharacters(string? name, bool expected)
    {
        Assert.Equal(expected, new CreateRoleCommandValidator().Validate(new CreateRoleCommand(name!, null)).IsValid);
        Assert.Equal(expected,
            new UpdateRoleByIdCommandValidator().Validate(new UpdateRoleByIdCommand(1, name!, null)).IsValid);
    }

    [Theory]
    [InlineData("a@b.com", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void AssignmentCommands_RequireUser(string? user, bool expected)
    {
        Assert.Equal(expected, new AssignUserCommandValidator().Validate(new AssignUserCommand(1, user!)).IsValid);
        Assert.Equal(expected, new UnassignUserCommandValidator().Validate(new UnassignUserCommand(1, user!)).IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(10.5, true)]
    [InlineData(-0.01, false)]
    public void PriceCommands_RejectNegativeValue(double value, bool expected)
    {
        var amount = (decimal)value;

        Assert.Equal(expected, new PriceUpdate.UpdateProductPriceByIdCommandValidator()
            .Validate(new PriceUpdate.UpdateProductPriceByIdCommand(SomeId, amount)).IsValid);
        Assert.Equal(expected, new ProductPriceUpdate.UpdateProductPriceByIdCommandValidator()
            .Validate(new ProductPriceUpdate.UpdateProductPriceByIdCommand(SomeId, amount)).IsValid);
    }

    [Fact]
    public void CreateProduct_WithValidInput_Passes()
    {
        Assert.True(Validate(ValidCreateProduct()).IsValid);
    }

    public static TheoryData<CreateProductCommand, string> InvalidCreateProducts => new()
    {
        { ValidCreateProduct() with { Name = "" }, "Name" },
        { ValidCreateProduct() with { BrandId = 0 }, "BrandId" },
        { ValidCreateProduct() with { BrandId = -1 }, "BrandId" },
        { ValidCreateProduct() with { UnitId = 0 }, "UnitId" },
        { ValidCreateProduct() with { UnitId = -1 }, "UnitId" },
        { ValidCreateProduct() with { Measure = -0.1f }, "Measure" },
        { ValidCreateProduct() with { Price = -0.01m }, "Price" },
        { ValidCreateProduct() with { Tags = [""] }, "Tags" },
        { ValidCreateProduct() with { Tags = ["ok", null!] }, "Tags" },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateProducts))]
    public void CreateProduct_WithInvalidField_ReportsThatField(CreateProductCommand command, string field)
    {
        AssertFails(Validate(command), field);
    }

    [Fact]
    public void CreateProduct_WithNullTags_FailsValidation()
    {
        AssertFails(Validate(ValidCreateProduct() with { Tags = null! }), "Tags");
    }

    [Fact]
    public void UpdateProduct_WithValidInput_Passes()
    {
        Assert.True(Validate(ValidUpdateProduct()).IsValid);
    }

    public static TheoryData<UpdateProductByIdCommand, string> InvalidUpdateProducts => new()
    {
        { ValidUpdateProduct() with { Id = Guid.Empty }, "Id" },
        { ValidUpdateProduct() with { Name = "" }, "Name" },
        { ValidUpdateProduct() with { UnitId = 0 }, "UnitId" },
        { ValidUpdateProduct() with { Measure = -1f }, "Measure" },
        { ValidUpdateProduct() with { Price = -1m }, "Price" },
        { ValidUpdateProduct() with { Tags = [" ok", ""] }, "Tags" },
    };

    [Theory]
    [MemberData(nameof(InvalidUpdateProducts))]
    public void UpdateProduct_WithInvalidField_ReportsThatField(UpdateProductByIdCommand command, string field)
    {
        AssertFails(Validate(command), field);
    }

    [Fact]
    public void CreatePurchase_WithValidInput_Passes()
    {
        Assert.True(new CreatePurchaseCommandValidator().Validate(ValidCreatePurchase()).IsValid);
    }

    public static TheoryData<CreatePurchaseCommand, string> InvalidCreatePurchases => new()
    {
        { ValidCreatePurchase() with { Title = "" }, "Title" },
        { ValidCreatePurchase() with { Tags = [""] }, "Tags" },
        { ValidCreatePurchase() with { OccurrenceTime = default }, "OccurrenceTime" },
        { ValidCreatePurchase() with { ProductEntries = [null!] }, "ProductEntries" },
        { ValidCreatePurchase() with { ProductEntries = [new PurchaseCreateEntry(SomeId, 0)] }, "ProductEntries" },
        { ValidCreatePurchase() with { ProductEntries = [new PurchaseCreateEntry(SomeId, -1)] }, "ProductEntries" },
    };

    [Theory]
    [MemberData(nameof(InvalidCreatePurchases))]
    public void CreatePurchase_WithInvalidField_ReportsThatField(CreatePurchaseCommand command, string field)
    {
        AssertFails(new CreatePurchaseCommandValidator().Validate(command), field);
    }

    [Fact]
    public void CreateSale_WithValidInput_Passes()
    {
        Assert.True(new CreateSaleCommandValidator().Validate(ValidCreateSale()).IsValid);
    }

    public static TheoryData<CreateSaleCommand, string> InvalidCreateSales => new()
    {
        { ValidCreateSale() with { Title = "" }, "Title" },
        { ValidCreateSale() with { Tags = [""] }, "Tags" },
        { ValidCreateSale() with { OccurrenceTime = default }, "OccurrenceTime" },
        { ValidCreateSale() with { ProductEntries = [null!] }, "ProductEntries" },
        { ValidCreateSale() with { ProductEntries = [new SaleCreateEntry(SomeId, 0)] }, "ProductEntries" },
        { ValidCreateSale() with { ProductEntries = [new SaleCreateEntry(SomeId, -1)] }, "ProductEntries" },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateSales))]
    public void CreateSale_WithInvalidField_ReportsThatField(CreateSaleCommand command, string field)
    {
        AssertFails(new CreateSaleCommandValidator().Validate(command), field);
    }

    [Fact]
    public void CreatePurchase_WithNullEntries_FailsValidation()
    {
        var command = ValidCreatePurchase() with { ProductEntries = null! };

        AssertFails(new CreatePurchaseCommandValidator().Validate(command), "ProductEntries");
    }

    [Fact]
    public void CreateSale_WithNullEntries_FailsValidation()
    {
        AssertFails(new CreateSaleCommandValidator().Validate(ValidCreateSale() with { ProductEntries = null! }),
            "ProductEntries");
    }

    [Fact]
    public void UpdateProduct_WithNullTags_FailsValidation()
    {
        AssertFails(Validate(ValidUpdateProduct() with { Tags = null! }), "Tags");
    }

    [Fact]
    public void CreatePurchaseAndSale_WithNullTags_FailValidation()
    {
        AssertFails(new CreatePurchaseCommandValidator().Validate(ValidCreatePurchase() with { Tags = null! }), "Tags");
        AssertFails(new CreateSaleCommandValidator().Validate(ValidCreateSale() with { Tags = null! }), "Tags");
    }

    [Fact]
    public void UpdatePurchaseAndSaleDetails_WithNullTags_FailValidation()
    {
        AssertFails(new UpdatePurchaseDetailsByIdCommandValidator()
            .Validate(new UpdatePurchaseDetailsByIdCommand(SomeId, "t", null!, When)), "Tags");
        AssertFails(new UpdateSaleDetailsByIdCommandValidator()
            .Validate(new UpdateSaleDetailsByIdCommand(SomeId, "t", null!, When)), "Tags");
    }

    [Fact]
    public void UpdatePurchaseAndSaleEntries_WithNullEntries_FailValidation()
    {
        AssertFails(new UpdatePurchaseEntriesByIdCommandValidator()
            .Validate(new UpdatePurchaseEntriesByIdCommand(SomeId, null!)), "ProductEntries");
        AssertFails(new UpdateSaleEntriesByIdCommandValidator()
            .Validate(new UpdateSaleEntriesByIdCommand(SomeId, null!)), "ProductEntries");
    }

    [Theory]
    [InlineData("Title", true, true)]
    [InlineData("", true, false)]
    [InlineData("Title", false, false)]
    public void UpdatePurchaseAndSaleDetails_RequireTitleAndOccurrence(string title, bool hasTime, bool expected)
    {
        var time = hasTime ? When : default;

        Assert.Equal(expected, new UpdatePurchaseDetailsByIdCommandValidator()
            .Validate(new UpdatePurchaseDetailsByIdCommand(SomeId, title, [], time)).IsValid);
        Assert.Equal(expected, new UpdateSaleDetailsByIdCommandValidator()
            .Validate(new UpdateSaleDetailsByIdCommand(SomeId, title, [], time)).IsValid);
    }

    [Fact]
    public void UpdatePurchaseAndSaleDetails_RejectEmptyTag()
    {
        AssertFails(new UpdatePurchaseDetailsByIdCommandValidator()
            .Validate(new UpdatePurchaseDetailsByIdCommand(SomeId, "t", [""], When)), "Tags");
        AssertFails(new UpdateSaleDetailsByIdCommandValidator()
            .Validate(new UpdateSaleDetailsByIdCommand(SomeId, "t", [""], When)), "Tags");
    }

    [Theory]
    [InlineData(true, 1, true)]
    [InlineData(false, 1, false)]
    [InlineData(true, 0, false)]
    [InlineData(true, -5, false)]
    public void UpdatePurchaseAndSaleEntries_RequireProductAndPositiveQuantity(bool hasProduct, int quantity,
        bool expected)
    {
        var productId = hasProduct ? SomeId : Guid.Empty;

        Assert.Equal(expected, new UpdatePurchaseEntriesByIdCommandValidator()
            .Validate(new UpdatePurchaseEntriesByIdCommand(SomeId,
                [new PurchaseUpdateEntry(null, productId, quantity)])).IsValid);
        Assert.Equal(expected, new UpdateSaleEntriesByIdCommandValidator()
            .Validate(new UpdateSaleEntriesByIdCommand(SomeId,
                [new SaleUpdateEntry(null, productId, quantity)])).IsValid);
    }

    [Fact]
    public void Entries_WithTotalQuantityTooLargeToAddTogether_FailValidation()
    {
        AssertFails(new CreatePurchaseCommandValidator().Validate(ValidCreatePurchase() with
        {
            ProductEntries = [new PurchaseCreateEntry(SomeId, int.MaxValue), new PurchaseCreateEntry(SomeId, 1)],
        }), "ProductEntries");
        AssertFails(new CreateSaleCommandValidator().Validate(ValidCreateSale() with
        {
            ProductEntries = [new SaleCreateEntry(SomeId, int.MaxValue), new SaleCreateEntry(SomeId, 1)],
        }), "ProductEntries");
        AssertFails(new UpdatePurchaseEntriesByIdCommandValidator().Validate(
            new UpdatePurchaseEntriesByIdCommand(SomeId,
            [new PurchaseUpdateEntry(null, SomeId, int.MaxValue), new PurchaseUpdateEntry(null, SomeId, 1)])),
            "ProductEntries");
        AssertFails(new UpdateSaleEntriesByIdCommandValidator().Validate(new UpdateSaleEntriesByIdCommand(SomeId,
            [new SaleUpdateEntry(null, SomeId, int.MaxValue), new SaleUpdateEntry(null, SomeId, 1)])),
            "ProductEntries");
    }

    [Fact]
    public void UpdatePurchaseAndSaleEntries_RejectNullEntry()
    {
        AssertFails(new UpdatePurchaseEntriesByIdCommandValidator()
            .Validate(new UpdatePurchaseEntriesByIdCommand(SomeId, [null!])), "ProductEntries");
        AssertFails(new UpdateSaleEntriesByIdCommandValidator()
            .Validate(new UpdateSaleEntriesByIdCommand(SomeId, [null!])), "ProductEntries");
    }

    [Theory]
    [InlineData("a@b.com", "x", true)]
    [InlineData("", "x", false)]
    [InlineData(null, "x", false)]
    [InlineData("not-an-email", "x", false)]
    [InlineData("a@b.com", "", false)]
    [InlineData("a@b.com", null, false)]
    public void Login_RequiresEmailAndPassword(string? email, string? password, bool expected)
    {
        Assert.Equal(expected, new LoginValidator().Validate(new LoginUserCommand(email!, password!)).IsValid);
    }

    [Theory]
    [InlineData("Passw0rd!", null)]
    [InlineData("", "Password")]
    [InlineData(null, "Password")]
    [InlineData("Pw0rd!a", "Password")]
    [InlineData("PASSW0RD!", "lowercase")]
    [InlineData("passw0rd!", "uppercase")]
    [InlineData("Password!", "number")]
    [InlineData("Passw0rdd", "special")]
    public void Register_EnforcesPasswordRules(string? password, string? expectedFragment)
    {
        var result = new RegisterValidator().Validate(new RegisterUserCommand("a@b.com", password!, "Jane", null));

        if (expectedFragment == null)
        {
            Assert.True(result.IsValid);
            return;
        }

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password" && e.ErrorMessage.Contains(expectedFragment));
    }

    [Theory]
    [InlineData("", "Email")]
    [InlineData("not-an-email", "Email")]
    public void Register_RejectsInvalidEmail(string email, string field)
    {
        AssertFails(new RegisterValidator().Validate(new RegisterUserCommand(email, "Passw0rd!", "Jane", null)), field);
    }

    [Theory]
    [InlineData("Jan", true)]
    [InlineData("Ja", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void UserNames_RequireAtLeastThreeCharacters(string? first, bool expected)
    {
        Assert.Equal(expected, new RegisterValidator()
            .Validate(new RegisterUserCommand("a@b.com", "Passw0rd!", first!, null)).IsValid);
        Assert.Equal(expected, new UpdateUserDetailByIdCommandValidator()
            .Validate(new UpdateUserDetailByIdCommand(SomeId, first!, null)).IsValid);
    }

    private static CreateProductCommand ValidCreateProduct()
    {
        return new CreateProductCommand("Milk", null, 1f, 1, 1, 1m, ["Dairy"]);
    }

    private static UpdateProductByIdCommand ValidUpdateProduct()
    {
        return new UpdateProductByIdCommand(SomeId, "Milk", null, 1f, 1, 1m, ["Dairy"]);
    }

    private static CreatePurchaseCommand ValidCreatePurchase()
    {
        return new CreatePurchaseCommand("Title", ["Tag"], [new PurchaseCreateEntry(SomeId, 1)], When);
    }

    private static CreateSaleCommand ValidCreateSale()
    {
        return new CreateSaleCommand("Title", ["Tag"], [new SaleCreateEntry(SomeId, 1)], When);
    }

    private static FluentValidation.Results.ValidationResult Validate(CreateProductCommand command)
    {
        return new CreateProductCommandValidator().Validate(command);
    }

    private static FluentValidation.Results.ValidationResult Validate(UpdateProductByIdCommand command)
    {
        return new UpdateProductByIdCommandValidator().Validate(command);
    }

    private static void AssertFails(FluentValidation.Results.ValidationResult result, string field)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == field);
    }
}
