namespace Application.Reports.DailyTotals;

public sealed record DailyTotalsResponse(
    DateOnly From, DateOnly To, string TimeZone,
    decimal SalesTotal, int SalesCount,
    decimal PurchasesTotal, int PurchasesCount,
    decimal CostOfSales, decimal GrossProfit, long UncostedQuantity,
    DayTotalsResponse[] Days
);

/// <summary>
/// One day's totals. Sales are valued at their selling price and purchases at their unit cost.
/// <see cref="CostOfSales"/> is what the items sold cost by moving average, and <see cref="GrossProfit"/> is their
/// selling price minus that cost. Both leave out items sold before any purchase of them, which have no known cost;
/// <see cref="UncostedQuantity"/> counts those, so a client can say the profit is incomplete.
/// </summary>
public sealed record DayTotalsResponse(
    DateOnly Date,
    decimal SalesTotal, int SalesCount,
    decimal PurchasesTotal, int PurchasesCount,
    decimal CostOfSales, decimal GrossProfit, long UncostedQuantity
);
