using Domain.Brands;

namespace Domain.Products;

public sealed class ProductDetail
{
    public Guid ProductId { get; private set; }
    public string? Description { get; private set; }
    public int BrandId { get; private set; }
    public float Measure { get; private set; }
    public int MeasureUnitId { get; private set; }

    // navs
    public Product Product { get; private set; } = default!;
    public Brand? Brand { get; private set; } = default!;
    public ProductUnit? MeasureUnit { get; private set; } = default!;

    private ProductDetail() { }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void UpdateMeasure(float value, ProductUnit unit)
    {
        Measure = value;
        MeasureUnit = unit;
    }

    public static ProductDetail CreateNew(Guid productId, string? description,
        int brandId, float measure, int measureUnitId)
    {
        return new()
        {
            ProductId = productId,
            Description = description,
            BrandId = brandId,
            Measure = measure,
            MeasureUnitId = measureUnitId,
        };
    }
}