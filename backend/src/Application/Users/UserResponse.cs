namespace Application.Users;

public sealed record UserResponse(Guid UserId, string Email, string FirstName, string? LastName);