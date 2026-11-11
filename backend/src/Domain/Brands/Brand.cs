namespace Domain.Brands;

public sealed class Brand
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedTime { get; private set; }

    private Brand() { }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public static Brand CreateNew(string name, DateTime CreateTime)
    {
        return new()
        {
            Name = name,
            CreatedTime = CreateTime
        };
    }
}