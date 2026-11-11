using Domain.Common;

namespace Domain.Products;

public sealed class ProductTag : Tag
{
    // navs
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private ProductTag() { }

    public static ProductTag CreateNew(string name)
    {
        return new()
        {
            Name = Name.CreateNew(name)
        };
    }
}