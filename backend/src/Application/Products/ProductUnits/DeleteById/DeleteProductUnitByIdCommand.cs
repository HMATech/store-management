using Application.Abstractions.Messaging;

namespace Application.Products.ProductUnits.DeleteById;

public sealed record DeleteProductUnitByIdCommand(int Id)
    : ICommand;