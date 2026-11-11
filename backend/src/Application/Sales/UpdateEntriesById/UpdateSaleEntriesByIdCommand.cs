using Application.Abstractions.Messaging;

namespace Application.Sales.UpdateEntriesById;

public sealed record UpdateSaleEntriesByIdCommand(
    Guid Id, List<ProductEntryCommand> ProductEntries
) : ICommand<UpdateSaleEntriesByIdResponse>;

public sealed record ProductEntryCommand(Guid? Id, Guid ProductId, int Quantity);