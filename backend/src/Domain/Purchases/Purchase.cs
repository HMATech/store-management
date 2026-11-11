namespace Domain.Purchases;

public sealed class Purchase
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public ICollection<PurchaseProductEntry> Products { get; private set; } = new List<PurchaseProductEntry>();
    public ICollection<PurchaseTag> Tags { get; private set; } = new List<PurchaseTag>();
    public DateTime OccurenceTime { get; private set; }
    public DateTime CreatedTime { get; private set; }
    public DateTime LastUpdatedTime { get; private set; }

    private Purchase() { }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateTags(List<PurchaseTag> tags)
    {
        Tags = tags;
    }

    public void UpdateProductEntries(List<PurchaseProductEntry> products)
    {
        Products = products;
    }

    public void UpdateOccurrenceTime(DateTime dateTime)
    {
        OccurenceTime = dateTime;
    }

    public void UpdateLastUpdatedTime(DateTime dateTime)
    {
        LastUpdatedTime = dateTime;
    }

    public static Purchase CreateNew(string title, DateTime occurenceTime, DateTime createdTime)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            OccurenceTime = occurenceTime,
            CreatedTime = createdTime,
            LastUpdatedTime = createdTime,
        };
    }
}