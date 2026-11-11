using Application.Abstractions.Messaging;

namespace Application.Products.ProductUnits.Create;

public sealed record CreateProductUnitCommand(string Name) : ICommand<CreateProductUnitResponse>;