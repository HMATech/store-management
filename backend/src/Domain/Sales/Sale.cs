namespace Domain.Sales;

public sealed class Sale
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public ICollection<SaleProductEntry> Products { get; private set; } = new List<SaleProductEntry>();
    public ICollection<SaleTag> Tags { get; private set; } = new List<SaleTag>();
    public DateTime OccurenceTime { get; private set; }
    public DateTime CreatedTime { get; private set; }
    public DateTime LastUpdatedTime { get; private set; }

    private Sale() { }

    public void UpdateTags(List<SaleTag> tags)
    {
        Tags = tags;
    }

    public void UpdateProductEntries(List<SaleProductEntry> productEntries)
    {
        Products = productEntries;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateOccurrenceTime(DateTime dateTime)
    {
        OccurenceTime = dateTime;
    }

    public void UpdateLastUpdatedTime(DateTime dateTime)
    {
        LastUpdatedTime = dateTime;
    }

    public static Sale CreateNew(string title,
        DateTime occurenceTime, DateTime createdTime)
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