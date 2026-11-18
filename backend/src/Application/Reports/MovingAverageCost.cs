namespace Application.Reports;

/// <summary>
/// Works out what each sold item cost the store, by the moving average of what was paid for it. Each purchase
/// updates a product's average from the stock on hand and the new goods, and each sale is costed at the average at
/// that moment. Nothing is stored: the history is replayed on each read, so it is right after any edit.
/// </summary>
internal static class MovingAverageCost
{
    /// <summary>The order of movements at the same moment: goods come in before they are sold.</summary>
    public enum MovementKind
    {
        Purchase = 0,
        Adjustment = 1,
        Sale = 2,
    }

    /// <summary>
    /// A change to a product's stock. <paramref name="Quantity"/> is positive for purchases and sales, and signed for
    /// adjustments. <paramref name="UnitCost"/> is used only for purchases. <paramref name="Id"/> is the entry's id,
    /// which a sale's cost is reported under.
    /// </summary>
    public sealed record Movement(Guid Id, Guid ProductId, DateTime Time, MovementKind Kind, long Quantity,
        decimal UnitCost = 0m);

    /// <summary>
    /// The unit cost of each sale movement, by its id. A sale made before the product's first purchase has no known
    /// cost and maps to <see langword="null"/>.
    /// </summary>
    public static Dictionary<Guid, decimal?> UnitCostOfSales(IEnumerable<Movement> movements)
    {
        var costs = new Dictionary<Guid, decimal?>();

        foreach (var product in movements.GroupBy(m => m.ProductId))
        {
            long onHand = 0;
            decimal? average = null;

            foreach (var movement in InTimeOrder(product))
            {
                switch (movement.Kind)
                {
                    case MovementKind.Purchase:
                        // Stock at or below zero has no cost left to average with, so the new goods set it.
                        average = average == null || onHand <= 0
                            ? movement.UnitCost
                            : (onHand * average.Value + movement.Quantity * movement.UnitCost)
                                / (onHand + movement.Quantity);
                        onHand += movement.Quantity;
                        break;
                    case MovementKind.Adjustment:
                        // Found or lost goods are valued at the average, so they change only how much is on hand.
                        onHand += movement.Quantity;
                        break;
                    case MovementKind.Sale:
                        costs[movement.Id] = average;
                        onHand -= movement.Quantity;
                        break;
                }
            }
        }

        return costs;
    }

    /// <summary>
    /// One product's movements oldest first, with purchases at the same moment, such as a paid line and a free one
    /// on the same delivery, joined into one at their average cost. Otherwise the order of those lines would change
    /// the result whenever the shelf was empty before them.
    /// </summary>
    private static IEnumerable<Movement> InTimeOrder(IEnumerable<Movement> movements)
    {
        return movements
            .GroupBy(m => (m.Time, m.Kind))
            .Select(g => g.Key.Kind == MovementKind.Purchase ? Joined(g.ToList()) : g.OrderBy(m => m.Id))
            .OrderBy(g => g.First().Time)
            .ThenBy(g => g.First().Kind)
            .SelectMany(g => g);
    }

    private static IEnumerable<Movement> Joined(List<Movement> purchases)
    {
        if (purchases.Count == 1)
        {
            return purchases;
        }

        var quantity = purchases.Sum(p => p.Quantity);
        var first = purchases[0];
        return
        [
            first with
            {
                Quantity = quantity,
                UnitCost = quantity == 0 ? first.UnitCost : purchases.Sum(p => p.Quantity * p.UnitCost) / quantity,
            },
        ];
    }
}
