using Application.Brands.Get;
using Application.Common;
using Application.Products.Get;
using Application.Products.ProductPrices.Get;
using Application.Purchases.Get;
using Application.Sales.Get;
using Application.Stocks.Get;
using Application.Stocks.StockAdjustments.Get;
using Application.Users.Get;
using FluentValidation;

namespace Application.Tests;

public class PagingValidatorTests
{
    public static TheoryData<int, int, bool> Paging => new()
    {
        { 1, 1, true },
        { 1, 10, true },
        { 500, 500, true },
        { 0, 10, false },
        { -1, 10, false },
        { 1, 0, false },
        { 1, -1, false },
        { 0, 0, false },
    };

    [Theory]
    [MemberData(nameof(Paging))]
    public void Brand_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetBrandQueryValidator(),
            new GetBrandQuery(null, SortOrder.ASC, page, pageSize)));
    }

    [Theory]
    [MemberData(nameof(Paging))]
    public void Product_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetProductQueryValidator(),
            new GetProductQuery(null, SortOrder.ASC, page, pageSize)));
    }

    [Theory]
    [MemberData(nameof(Paging))]
    public void ProductPrice_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetProductPriceQueryValidator(),
            new GetProductPriceQuery(null, SortOrder.ASC, page, pageSize)));
    }

    [Theory]
    [MemberData(nameof(Paging))]
    public void Purchase_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetPurchaseQueryValidator(),
            new GetPurchaseQuery(null, SortOrder.ASC, null, null, null, null, page, pageSize)));
    }

    [Theory]
    [MemberData(nameof(Paging))]
    public void Sale_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetSaleQueryValidator(),
            new GetSaleQuery(null, SortOrder.ASC, null, null, null, null, page, pageSize)));
    }

    [Theory]
    [MemberData(nameof(Paging))]
    public void User_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetUserQueryValidator(), new GetUserQuery(null, page, pageSize)));
    }

    [Theory]
    [MemberData(nameof(Paging))]
    public void Stock_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetStockQueryValidator(),
            new GetStockQuery(null, SortOrder.ASC, page, pageSize)));
    }

    [Theory]
    [MemberData(nameof(Paging))]
    public void StockAdjustment_ValidatesPaging(int page, int pageSize, bool expected)
    {
        Assert.Equal(expected, IsValid(new GetStockAdjustmentQueryValidator(),
            new GetStockAdjustmentQuery(null, page, pageSize)));
    }

    private static bool IsValid<T>(AbstractValidator<T> validator, T query)
    {
        return validator.Validate(query).IsValid;
    }
}
