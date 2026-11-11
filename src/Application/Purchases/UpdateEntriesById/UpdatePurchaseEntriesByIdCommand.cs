using Application.Abstractions.Messaging;

namespace Application.Purchases.UpdateEntriesById;

public sealed record UpdatePurchaseEntriesByIdCommand(
    Guid Id, List<ProductEntryCommand> ProductEntries
) : ICommand<UpdatePurchaseEntriesByIdResponse>;

public sealed record ProductEntryCommand(Guid? Id, Guid ProductId, int Quantity, decimal UnitCost);
