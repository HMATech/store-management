using Domain.Common;

namespace Domain.Sales;

public sealed class SaleTag : Tag
{
    // navs
    public ICollection<Sale> Sale { get; private set; } = new List<Sale>();

    private SaleTag() { }

    public static SaleTag CreateNew(string name)
    {
        return new()
        {
            Name = Name.CreateNew(name)
        };
    }
}