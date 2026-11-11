namespace Domain.Users;

public sealed class User
{
    public Guid Id { get; private set; } = Guid.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserName Name { get; private set; } = default!;
    public DateTime CreatedTime { get; private set; }

    private User() { }

    public void UpdateDetail(string firstName, string? lastName)
    {
        Name.UpdateFirst(firstName);
        Name.UpdateLast(lastName);
    }

    public static User CreateNew(string email, string passwordHash,
        string firstName, string? lastName, DateTime createdTime)
    {
        return new User()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Name = UserName.CreateNew(firstName, lastName),
            CreatedTime = createdTime
        };
    }
}