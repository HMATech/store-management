namespace Application.Reports.DailyTotals;

public sealed record DailyTotalsResponse(
    DateOnly From, DateOnly To, string TimeZone,
    decimal SalesTotal, int SalesCount,
    decimal PurchasesTotal, int PurchasesCount,
    DayTotalsResponse[] Days
);

/// <summary>One day's totals. Sales are valued at their selling price and purchases at their unit cost.</summary>
public sealed record DayTotalsResponse(
    DateOnly Date,
    decimal SalesTotal, int SalesCount,
    decimal PurchasesTotal, int PurchasesCount
);
