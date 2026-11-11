using Application.Abstractions.Messaging;

namespace Application.Purchases.Create;

public sealed record CreatePurchaseCommand(
    string Title,
    string[] Tags,
    List<ProductEntryCommand> ProductEntries,
    DateTime OccurrenceTime
) : ICommand<CreatePurchaseResponse>;

public sealed record ProductEntryCommand(Guid Id, int Quantity, decimal UnitCost);
