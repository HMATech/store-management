using Domain.Users;

namespace Domain.Roles;

public sealed class UserRole
{
    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public int RoleId { get; private set; }
    public DateTime AssignedTime { get; private set; }

    // navs
    public User? User { get; private set; }
    public Role? Role { get; private set; }

    private UserRole() { }

    public static UserRole CreateNew(Guid userId, int roleId, DateTime assignedTime)
    {
        return new()
        {
            UserId = userId,
            RoleId = roleId,
            AssignedTime = assignedTime
        };
    }
}