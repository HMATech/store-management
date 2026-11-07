using Domain.Brands;
using Domain.Products;
using Domain.Purchases;
using Domain.Roles;
using Domain.Sales;
using Domain.Stocks;
using Domain.Users;
using Shared;

namespace Domain.Tests;

public class ErrorCatalogTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static TheoryData<Error, string, ErrorType, string> Errors => new()
    {
        { BrandErrors.AlreadyExists("x"), "Brands.AlreadyExist", ErrorType.Conflict, "x" },
        { BrandErrors.InUse(5), "Brands.InUse", ErrorType.Conflict, "5" },
        { BrandErrors.NotFound(5), "Brands.NotFound", ErrorType.NotFound, "5" },
        { BrandErrors.NotFound("x"), "Brands.NotFound", ErrorType.NotFound, "x" },
        { UserErrors.NotFound(Id), "Users.NotFound", ErrorType.NotFound, Id.ToString() },
        { UserErrors.NotFound("a@b.com"), "Users.NotFound", ErrorType.NotFound, "a@b.com" },
        { UserErrors.EmailAlreadyRegistered("a@b.com"), "Users.EmailAlreadyRegistered", ErrorType.Conflict, "a@b.com" },
        { UserErrors.IncorrectCredential(), "Users.IncorrectCredential", ErrorType.Problem, "Incorrect" },
        { RoleErrors.NotFound(5), "Roles.NotFound", ErrorType.NotFound, "5" },
        { RoleErrors.NotFound("x"), "Roles.NotFound", ErrorType.NotFound, "x" },
        { RoleErrors.AlreadyExist("x"), "Roles.AlreadyExist", ErrorType.Conflict, "x" },
        { RoleErrors.AlreadyAssigned(5, "u"), "Roles.AlreadyAssigned", ErrorType.Conflict, "u" },
        { RoleErrors.NotAssigned(5, "u"), "Roles.NotAssigned", ErrorType.Problem, "u" },
        { ProductErrors.NotFound(Id), "Products.NotFound", ErrorType.NotFound, Id.ToString() },
        { ProductErrors.AlreadyExists("n", "b", 5), "Products.AlreadyExist", ErrorType.Conflict, "b (id: 5)" },
        { ProductErrors.InUse(Id), "Products.InUse", ErrorType.Conflict, Id.ToString() },
        { ProductErrors.PriceNotFound(Id), "Products.PriceNotFound", ErrorType.NotFound, Id.ToString() },
        { ProductErrors.PriceInUse(Id), "Products.PriceInUse", ErrorType.Conflict, Id.ToString() },
        { ProductErrors.UnitNotFound(5), "Products.UnitNotFound", ErrorType.NotFound, "5" },
        { ProductErrors.UnitNotFound("x"), "Products.UnitNotFound", ErrorType.NotFound, "x" },
        { ProductErrors.UnitAlreadyExists("x"), "Products.UnitAlreadyExists", ErrorType.Conflict, "x" },
        { ProductErrors.UnitIsStillInUse(5), "Products.UnitIsStillReferenced", ErrorType.Conflict, "5" },
        { ProductErrors.TagNotFound(5), "Products.TagNotFound", ErrorType.NotFound, "5" },
        { ProductErrors.TagAlreadyExists("x"), "Products.TagAlreadyExists", ErrorType.Conflict, "x" },
        { StockErrors.AdjustmentNotFound(Id), "Stocks.AdjustmentNotFound", ErrorType.NotFound, Id.ToString() },
        { PurchaseErrors.NotFound(Id), "Purchase.NotFound", ErrorType.NotFound, Id.ToString() },
        {
            PurchaseErrors.ContainNonExistingProducts([Id]), "Purchase.NonExistingProduct(s)",
            ErrorType.NotFound, Id.ToString()
        },
        {
            PurchaseErrors.ContainForeignEntries(Guid.Empty, [Id]), "Purchase.NonExistingEntry(s)",
            ErrorType.NotFound, Id.ToString()
        },
        { SaleErrors.NotFound(Id), "Sale.NotFound", ErrorType.NotFound, Id.ToString() },
        {
            SaleErrors.ContainNonExistingProducts([Id]), "Sale.NonExistingProduct(s)",
            ErrorType.NotFound, Id.ToString()
        },
        {
            SaleErrors.ContainForeignEntries(Guid.Empty, [Id]), "Sale.NonExistingEntry(s)",
            ErrorType.NotFound, Id.ToString()
        },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void Error_HasExpectedCodeTypeAndDescription(Error error, string code, ErrorType type, string fragment)
    {
        Assert.Equal(code, error.Code);
        Assert.Equal(type, error.ErrorType);
        Assert.Contains(fragment, error.Description);
    }
}
