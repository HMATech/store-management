namespace Application.Users.Register;

public sealed record RegisterResponse(Guid Id, string Email, string FirstName, string? LastName);