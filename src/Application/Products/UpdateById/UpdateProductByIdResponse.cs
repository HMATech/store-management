namespace Application.Products.UpdateById;

public sealed record UpdateProductByIdResponse(
    Guid Id, string Name, string? Description,
    float Measure, string Unit,
    decimal Price, string[] Tags, StockDefinitionResponse StockDefinition
);