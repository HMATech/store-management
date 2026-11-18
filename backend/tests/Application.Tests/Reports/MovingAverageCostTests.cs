using Application.Reports;
using static Application.Reports.MovingAverageCost;

namespace Application.Tests.Reports;

public class MovingAverageCostTests
{
    private static readonly Guid Product = Guid.NewGuid();
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UnitCostOfSales_CostsASaleAtTheOnlyPurchasesCost()
    {
        var sale = Sale(Start.AddHours(1), 3);

        var costs = UnitCostOfSales([Purchase(Start, 10, 2500m), sale]);

        Assert.Equal(2500m, costs[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_WeighsTheStockOnHandAgainstTheNewPurchase()
    {
        // 10 at 3.000, 4 sold, then 12 at 3.300: (6 x 3.000 + 12 x 3.300) / 18 = 3.200.
        var before = Sale(Start.AddHours(1), 4);
        var after = Sale(Start.AddHours(3), 5);

        var costs = UnitCostOfSales(
            [Purchase(Start, 10, 3000m), before, Purchase(Start.AddHours(2), 12, 3300m), after]);

        Assert.Equal(3000m, costs[before.Id]);
        Assert.Equal(3200m, costs[after.Id]);
    }

    [Fact]
    public void UnitCostOfSales_LetsAPurchaseSetTheCostWhenTheShelfIsEmpty()
    {
        var sale = Sale(Start.AddHours(3), 1);

        var costs = UnitCostOfSales(
            [Purchase(Start, 2, 1000m), Sale(Start.AddHours(1), 2), Purchase(Start.AddHours(2), 5, 1500m), sale]);

        Assert.Equal(1500m, costs[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_LetsAPurchaseSetTheCostWhenStockIsNegative()
    {
        // Sold 3 more than were recorded as bought: the next purchase has no stock to average with.
        var sale = Sale(Start.AddHours(3), 1);

        var costs = UnitCostOfSales(
            [Purchase(Start, 2, 1000m), Sale(Start.AddHours(1), 5), Purchase(Start.AddHours(2), 10, 1400m), sale]);

        Assert.Equal(1400m, costs[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_HasNoCostForASaleBeforeTheFirstPurchase()
    {
        var early = Sale(Start, 2);
        var later = Sale(Start.AddHours(2), 1);

        var costs = UnitCostOfSales([early, Purchase(Start.AddHours(1), 6, 4000m), later]);

        Assert.Null(costs[early.Id]);
        Assert.Equal(4000m, costs[later.Id]);
    }

    [Fact]
    public void UnitCostOfSales_CountsAdjustmentsInTheStockOnHandWithoutChangingTheAverage()
    {
        // 10 at 2.000 with 6 thrown away leaves 4. 4 more at 3.000 average (4 x 2.000 + 4 x 3.000) / 8 = 2.500.
        var sale = Sale(Start.AddHours(3), 1);

        var costs = UnitCostOfSales(
        [
            Purchase(Start, 10, 2000m),
            Adjustment(Start.AddHours(1), -6),
            Purchase(Start.AddHours(2), 4, 3000m),
            sale,
        ]);

        Assert.Equal(2500m, costs[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_TakesGoodsInBeforeSellingThemAtTheSameMoment()
    {
        var sale = Sale(Start, 1);

        var costs = UnitCostOfSales([sale, Purchase(Start, 1, 900m)]);

        Assert.Equal(900m, costs[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_ReadsTheMovementsInTimeOrderWhateverOrderTheyCome()
    {
        var sale = Sale(Start.AddHours(3), 1);

        var costs = UnitCostOfSales(
            [sale, Purchase(Start.AddHours(2), 2, 4000m), Purchase(Start, 2, 2000m)]);

        Assert.Equal(3000m, costs[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_KeepsEachProductsAverageApart()
    {
        var other = Guid.NewGuid();
        var sale = Sale(Start.AddHours(2), 1);
        var otherSale = new Movement(Guid.NewGuid(), other, Start.AddHours(2), MovementKind.Sale, 1);

        var costs = UnitCostOfSales(
        [
            Purchase(Start, 5, 1000m),
            new Movement(Guid.NewGuid(), other, Start.AddHours(1), MovementKind.Purchase, 5, 8000m),
            sale,
            otherSale,
        ]);

        Assert.Equal(1000m, costs[sale.Id]);
        Assert.Equal(8000m, costs[otherSale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_LetsAFreeItemLowerTheAverage()
    {
        // A bonus carton from the supplier: 10 paid at 2.400 and 2 free average 2.000.
        var sale = Sale(Start.AddHours(2), 1);

        var costs = UnitCostOfSales([Purchase(Start, 10, 2400m), Purchase(Start.AddHours(1), 2, 0m), sale]);

        Assert.Equal(2000m, costs[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_JoinsPurchasesAtTheSameMomentWhateverTheirOrder()
    {
        // The shelf is 5 short when 40 paid at 3.000 and 40 free arrive together: 80 at an average of 1.500.
        var paid = new Movement(Guid.Empty, Product, Start.AddHours(2), MovementKind.Purchase, 40, 3000m);
        var free = paid with { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), UnitCost = 0m };
        var sale = Sale(Start.AddHours(3), 1);
        Movement[] before = [Purchase(Start, 5, 2000m), Sale(Start.AddHours(1), 10)];

        var paidFirst = UnitCostOfSales([.. before, paid, free, sale]);
        var freeFirst = UnitCostOfSales([.. before, free with { Id = Guid.Empty }, paid with { Id = free.Id }, sale]);

        Assert.Equal(1500m, paidFirst[sale.Id]);
        Assert.Equal(1500m, freeFirst[sale.Id]);
    }

    [Fact]
    public void UnitCostOfSales_WithNoMovements_IsEmpty()
    {
        Assert.Empty(UnitCostOfSales([]));
    }

    private static Movement Purchase(DateTime time, long quantity, decimal unitCost)
    {
        return new Movement(Guid.NewGuid(), Product, time, MovementKind.Purchase, quantity, unitCost);
    }

    private static Movement Sale(DateTime time, long quantity)
    {
        return new Movement(Guid.NewGuid(), Product, time, MovementKind.Sale, quantity);
    }

    private static Movement Adjustment(DateTime time, long quantity)
    {
        return new Movement(Guid.NewGuid(), Product, time, MovementKind.Adjustment, quantity);
    }
}
