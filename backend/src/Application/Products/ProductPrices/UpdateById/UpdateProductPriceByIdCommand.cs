using Application.Abstractions.Messaging;

namespace Application.Products.ProductPrices.UpdateById;

public sealed record UpdateProductPriceByIdCommand(Guid Id, decimal Value)
    : ICommand<UpdateProductPriceByIdResponse>;