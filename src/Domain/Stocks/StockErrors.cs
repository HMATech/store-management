using Shared;

namespace Domain.Stocks;

public static class StockErrors
{
    public static Error AdjustmentNotFound(Guid id) => Error.NotFound(
        "Stocks.AdjustmentNotFound",
        $"Stock adjustment with id '{id}' was not found."
    );
}
