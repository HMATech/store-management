using Application.Abstractions.Messaging;

namespace Application.Products.UpdateById;

/// <summary>Leaving <paramref name="StockDefinition"/> out keeps the product's current stock rules.</summary>
public sealed record UpdateProductByIdCommand(Guid Id,
    string Name, string? Description, float Measure,
    int UnitId, decimal Price, string[] Tags,
    StockDefinitionCommand? StockDefinition = null
) : ICommand<UpdateProductByIdResponse>;
