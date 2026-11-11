namespace Application.Transactions;

/// <summary>
/// One sale or purchase in the combined history. <see cref="Total"/> is the selling-price total for a sale and the
/// cost total for a purchase. <see cref="LineCount"/> counts entry lines and <see cref="Quantity"/> counts units.
/// </summary>
public sealed record TransactionSummaryResponse(
    Guid Id, TransactionKind Kind, string Title, DateTime OccurrenceTime,
    int LineCount, long Quantity, decimal Total
);
