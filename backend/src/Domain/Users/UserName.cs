namespace Domain.Users;

public sealed class UserName
{
    public string First { get; private set; } = string.Empty;
    public string? Last { get; private set; }

    public void UpdateFirst(string name) => First = name;
    public void UpdateLast(string? name) => Last = name;

    private UserName() { }

    private UserName(string firstName, string? lastName)
        => (First, Last) = (firstName, lastName);

    public static UserName CreateNew(string firstName, string? lastName)
        => new(firstName, lastName);
}