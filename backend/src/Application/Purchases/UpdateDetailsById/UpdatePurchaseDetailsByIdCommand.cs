using Application.Abstractions.Messaging;

namespace Application.Purchases.UpdateDetailsById;

public sealed record UpdatePurchaseDetailsByIdCommand(
    Guid Id,
    string Title,
    string[] Tags,
    DateTime OccurrenceTime
) : ICommand<UpdatePurchaseDetailsByIdResponse>;