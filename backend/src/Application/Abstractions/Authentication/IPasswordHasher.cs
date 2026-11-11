using Shared;

namespace Application.Abstractions.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);

    Result Verify(string inputPassword, string hashedPassword);
}

public static class IPasswordHasherExtensions
{
    public const string InvalidInputCode = "IPasswordHasher.InvalidInput";
    public const string IncorrectPasswordCode = "IPasswordHasher.WrongPassword";
    public const string InvalidInputDetail = "Input password or hashed password cannot be null or empty";
    public const string IncorrectPasswordDetail = "Input password was incorrect";
}