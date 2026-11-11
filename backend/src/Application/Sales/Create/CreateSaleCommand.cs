using Application.Abstractions.Messaging;

namespace Application.Sales.Create;

public sealed record CreateSaleCommand(
    string Title,
    string[] Tags,
    List<ProductEntryCommand> ProductEntries,
    DateTime OccurrenceTime
) : ICommand<CreateSaleResponse>;

public sealed record ProductEntryCommand(Guid Id, int Quantity);