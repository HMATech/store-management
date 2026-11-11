using Application.Abstractions.Messaging;

namespace Application.Products.ProductUnits.UpdateById;

public sealed record UpdateProductUnitByIdCommand(int Id, string Name)
    : ICommand<UpdateProductUnitByIdResponse>;