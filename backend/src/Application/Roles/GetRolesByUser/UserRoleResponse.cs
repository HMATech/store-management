namespace Application.Roles.GetRolesByUser;

public sealed record UserRoleResponse(int RoleId, string RoleName,
    string RoleNormalizedName, DateTime AssignedTime);