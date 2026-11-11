using Application.Abstractions.Messaging;

namespace Application.Products.UpdatePriceById;

public sealed record UpdateProductPriceByIdCommand(Guid Id, decimal Value) 
    : ICommand<UpdateProductPriceByIdResponse>;