using Application.Abstractions.Messaging;

namespace Application.Products.Create;

public sealed record CreateProductCommand(
    string Name, string? Description, float Measure,
    int UnitId, int BrandId, decimal Price,
    string[] Tags, StockDefinitionCommand? StockDefinition = null
)
    : ICommand<CreateProductResponse>;
