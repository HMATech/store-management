using Application.Abstractions.Messaging;

namespace Application.Products.ProductPrices.DeleteById;

public sealed record DeleteProductPriceByIdCommand(Guid Id)
    : ICommand;