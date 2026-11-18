using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Reports.DailyTotals;

internal sealed class GetDailyTotalsQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetDailyTotalsQueryHandler> logger
) : IQueryHandler<GetDailyTotalsQuery, DailyTotalsResponse>
{
    private sealed record TransactionTotal(DateTime OccurrenceTime, decimal Total);

    private sealed record SoldLine(Guid Id, DateTime OccurrenceTime, Guid ProductId, long Quantity, decimal Price);

    private sealed record DayProfit(decimal CostOfSales, decimal GrossProfit, long UncostedQuantity);

    public async Task<Result<DailyTotalsResponse>> HandleAsync(GetDailyTotalsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(query.TimeZone);
            var startUtc = ToUtc(query.From, timeZone);
            var endUtc = ToUtc(query.To.AddDays(1), timeZone);

            // Each transaction's total is summed in SQL; only the per-day grouping happens here,
            // because the day depends on the time zone.
            var sales = await dbContext.Sales
                .Where(s => s.OccurenceTime >= startUtc && s.OccurenceTime < endUtc)
                .Select(s => new TransactionTotal(s.OccurenceTime,
                    s.Products.Sum(e => e.ProductPrice!.Value * e.Quantity)))
                .ToListAsync(cancellationToken);

            var purchases = await dbContext.Purchases
                .Where(p => p.OccurenceTime >= startUtc && p.OccurenceTime < endUtc)
                .Select(p => new TransactionTotal(p.OccurenceTime,
                    p.Products.Sum(e => e.UnitCost * e.Quantity)))
                .ToListAsync(cancellationToken);

            var soldLines = await dbContext.SaleProductEntries
                .Where(e => e.Sale.OccurenceTime >= startUtc && e.Sale.OccurenceTime < endUtc)
                .Select(e => new SoldLine(e.Id, e.Sale.OccurenceTime, e.ProductId, e.Quantity,
                    e.ProductPrice!.Value))
                .ToListAsync(cancellationToken);

            var salesByDay = GroupByLocalDay(sales, timeZone);
            var purchasesByDay = GroupByLocalDay(purchases, timeZone);
            var profitByDay = ProfitByLocalDay(soldLines,
                await UnitCostOfSalesAsync(soldLines, endUtc, cancellationToken), timeZone);

            var days = Enumerable.Range(0, query.To.DayNumber - query.From.DayNumber + 1)
                .Select(offset => query.From.AddDays(offset))
                .Select(date =>
                {
                    var (salesTotal, salesCount) = salesByDay.GetValueOrDefault(date);
                    var (purchasesTotal, purchasesCount) = purchasesByDay.GetValueOrDefault(date);
                    var profit = profitByDay.GetValueOrDefault(date) ?? new DayProfit(0m, 0m, 0);
                    return new DayTotalsResponse(date, salesTotal, salesCount, purchasesTotal, purchasesCount,
                        profit.CostOfSales, profit.GrossProfit, profit.UncostedQuantity);
                })
                .ToArray();

            return new DailyTotalsResponse(query.From, query.To, query.TimeZone,
                days.Sum(d => d.SalesTotal), days.Sum(d => d.SalesCount),
                days.Sum(d => d.PurchasesTotal), days.Sum(d => d.PurchasesCount),
                days.Sum(d => d.CostOfSales), days.Sum(d => d.GrossProfit), days.Sum(d => d.UncostedQuantity),
                days);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetDailyTotalsQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting daily totals with '{query}'", query);
            return ApplicationErrors.UnexpectedError(nameof(GetDailyTotalsQueryHandler),
                $"Unexpected error has occurred while getting daily totals with '{query}'");
        }
    }

    /// <summary>
    /// The moving average cost of each sold line, from the whole history of the products sold up to the end of the
    /// range. Later movements can't change the cost of an earlier sale, so they aren't loaded.
    /// </summary>
    private async Task<Dictionary<Guid, decimal?>> UnitCostOfSalesAsync(List<SoldLine> soldLines, DateTime endUtc,
        CancellationToken cancellationToken)
    {
        if (soldLines.Count == 0)
        {
            return [];
        }

        var productIds = soldLines.Select(l => l.ProductId).Distinct().ToList();

        var purchased = await dbContext.PurchaseProductEntries
            .Where(e => productIds.Contains(e.ProductId) && e.Purchase.OccurenceTime < endUtc)
            .Select(e => new MovingAverageCost.Movement(e.Id, e.ProductId, e.Purchase.OccurenceTime,
                MovingAverageCost.MovementKind.Purchase, e.Quantity, e.UnitCost))
            .ToListAsync(cancellationToken);

        var sold = await dbContext.SaleProductEntries
            .Where(e => productIds.Contains(e.ProductId) && e.Sale.OccurenceTime < endUtc)
            .Select(e => new MovingAverageCost.Movement(e.Id, e.ProductId, e.Sale.OccurenceTime,
                MovingAverageCost.MovementKind.Sale, e.Quantity, 0m))
            .ToListAsync(cancellationToken);

        var adjusted = await dbContext.StockAdjustments
            .Where(a => productIds.Contains(a.ProductId) && a.OccurrenceTime < endUtc)
            .Select(a => new MovingAverageCost.Movement(a.Id, a.ProductId, a.OccurrenceTime,
                MovingAverageCost.MovementKind.Adjustment, a.Quantity, 0m))
            .ToListAsync(cancellationToken);

        return MovingAverageCost.UnitCostOfSales(purchased.Concat(sold).Concat(adjusted));
    }

    private static DateTime ToUtc(DateOnly localDate, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            timeZone);
    }

    private static Dictionary<DateOnly, (decimal Total, int Count)> GroupByLocalDay(
        IEnumerable<TransactionTotal> transactions, TimeZoneInfo timeZone)
    {
        return transactions
            .GroupBy(t => LocalDay(t.OccurrenceTime, timeZone))
            .ToDictionary(g => g.Key, g => (g.Sum(t => t.Total), g.Count()));
    }

    /// <summary>
    /// Each day's cost of sales and gross profit, rounded to two decimals because averages rarely divide evenly.
    /// Lines without a known cost are counted apart, not guessed.
    /// </summary>
    private static Dictionary<DateOnly, DayProfit> ProfitByLocalDay(IEnumerable<SoldLine> soldLines,
        Dictionary<Guid, decimal?> unitCosts, TimeZoneInfo timeZone)
    {
        return soldLines
            .GroupBy(l => LocalDay(l.OccurrenceTime, timeZone))
            .ToDictionary(g => g.Key, g =>
            {
                var costed = g
                    .Select(l => (Line: l, UnitCost: unitCosts.GetValueOrDefault(l.Id)))
                    .Where(c => c.UnitCost != null)
                    .ToList();
                var cost = costed.Sum(c => c.UnitCost!.Value * c.Line.Quantity);
                var revenue = costed.Sum(c => c.Line.Price * c.Line.Quantity);
                return new DayProfit(Round(cost), Round(revenue - cost),
                    g.Sum(l => l.Quantity) - costed.Sum(c => c.Line.Quantity));
            });
    }

    private static DateOnly LocalDay(DateTime occurrenceTime, TimeZoneInfo timeZone)
    {
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(occurrenceTime, DateTimeKind.Utc), timeZone));
    }

    private static decimal Round(decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
