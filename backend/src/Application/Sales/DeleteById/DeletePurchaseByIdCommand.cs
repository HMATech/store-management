using Application.Abstractions.Messaging;

namespace Application.Sales.DeleteById;

public sealed record DeleteSaleByIdCommand(Guid Id) : ICommand;