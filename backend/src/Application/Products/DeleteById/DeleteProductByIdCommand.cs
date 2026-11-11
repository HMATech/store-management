using Application.Abstractions.Messaging;

namespace Application.Products.DeleteById;

public sealed record DeleteProductByIdCommand(Guid Id)
    : ICommand;