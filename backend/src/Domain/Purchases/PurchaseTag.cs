using Domain.Common;

namespace Domain.Purchases;

public sealed class PurchaseTag : Tag
{
    // navs
    public ICollection<Purchase> Purchases { get; private set; } = new List<Purchase>();
    
    private PurchaseTag() { }

    public static PurchaseTag CreateNew(string name)
    {
        return new()
        {
            Name = Name.CreateNew(name)
        };
    }
}