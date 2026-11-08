using Application.Common;
using Application.Products;
using Application.Products.Create;
using Application.Products.UpdateById;
using Application.Stocks.Get;

namespace Application.Tests.Products;

public class StockDefinitionValidationTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(25, true)]
    [InlineData(-1, false)]
    public void CreateAndUpdateProduct_AcceptOnlyANonNegativeMinimum(int? minimum, bool expected)
    {
        var definition = new StockDefinitionCommand(minimum);

        var create = new CreateProductCommandValidator().Validate(
            new CreateProductCommand("Milk", null, 1f, 1, 1, 1m, [], definition));
        var update = new UpdateProductByIdCommandValidator().Validate(
            new UpdateProductByIdCommand(Guid.NewGuid(), "Milk", null, 1f, 1, 1m, [], definition));

        Assert.Equal(expected, create.IsValid);
        Assert.Equal(expected, update.IsValid);
        if (!expected)
        {
            Assert.Contains(create.Errors, e => e.PropertyName == "StockDefinition.MinimumQuantity");
        }
    }

    [Fact]
    public void CreateProduct_WithoutStockDefinition_Passes()
    {
        Assert.True(new CreateProductCommandValidator().Validate(
            new CreateProductCommand("Milk", null, 1f, 1, 1, 1m, [])).IsValid);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("low", true)]
    [InlineData("Negative", true)]
    [InlineData("1", false)]
    [InlineData("empty", false)]
    public void GetStock_AcceptsOnlyStatusNames(string? status, bool expected)
    {
        var result = new GetStockQueryValidator().Validate(new GetStockQuery(null, SortOrder.ASC, Status: status));

        Assert.Equal(expected, result.IsValid);
    }
}
