namespace Application.Roles.AssignUser;

public sealed record AssignUserResponse(int RoleId, string RoleName, Guid UserId);