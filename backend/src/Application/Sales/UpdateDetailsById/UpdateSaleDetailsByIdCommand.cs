using Application.Abstractions.Messaging;

namespace Application.Sales.UpdateDetailsById;

public sealed record UpdateSaleDetailsByIdCommand(
    Guid Id,
    string Title,
    string[] Tags,
    DateTime OccurrenceTime
) : ICommand<UpdateSaleDetailsByIdResponse>;