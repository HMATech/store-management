using Application.Abstractions.Messaging;

namespace Application.Purchases.DeleteById;

public sealed record DeletePurchaseByIdCommand(Guid Id) : ICommand;