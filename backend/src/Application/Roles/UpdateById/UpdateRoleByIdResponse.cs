namespace Application.Roles.UpdateById;

public sealed record UpdateRoleByIdResponse(string Name,
    string NormalizedName, string? Description);