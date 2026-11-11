namespace Domain.Common;

public class Tag
{
    public int Id { get; protected set; }
    public Name Name { get; protected set; } = default!;

    public void UpdateName(string name)
    {
        Name = Name.CreateNew(name);
    }

    protected Tag() { }
}