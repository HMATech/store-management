using Domain.Common;

namespace Domain.Roles;

public sealed class Role
{
    public int Id { get; private set; }
    public Name Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime CreatedTime { get; private set; }

    private Role() { }

    public void UpdateName(string name)
    {
        Name = Name.CreateNew(name);
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public static Role CreateNew(string name, string? description,
        DateTime createdTime)
    {
        return new()
        {
            Name = Name.CreateNew(name),
            Description = description,
            CreatedTime = createdTime
        };
    }
}