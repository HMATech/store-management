namespace Application.Roles;

public sealed record RoleResponse(int Id, string Name,
    string NormalizedName, string? Description, DateTime CreatedTime);