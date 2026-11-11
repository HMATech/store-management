namespace Application.Products;

/// <summary>A product's stock rules. A rule left empty gets its default.</summary>
public sealed record StockDefinitionCommand(int? MinimumQuantity);
