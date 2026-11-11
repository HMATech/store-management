using System.Security.Cryptography;
using Application.Abstractions.Authentication;
using Shared;

namespace Infrastructure.Authentication;

internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int IterationCount = 100_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, IterationCount, Algorithm, HashSize);

        var combined = $"{Convert.ToHexString(hash)}.+{Convert.ToHexString(salt)}";
        return combined;
    }

    public Result Verify(string inputPassword, string hashedPassword)
    {
        if (string.IsNullOrEmpty(inputPassword)
            || string.IsNullOrEmpty(hashedPassword))
        {
            return Error.Problem(IPasswordHasherExtensions.InvalidInputCode,
                IPasswordHasherExtensions.InvalidInputDetail);
        }

        var splitted = hashedPassword.Split(".+");
        if (splitted.Length != 2)
        {
            throw new InvalidOperationException("Invalid hashed password");
        }

        var hash = Convert.FromHexString(splitted[0]);
        var salt = Convert.FromHexString(splitted[1]);

        var hashedInput = Rfc2898DeriveBytes.Pbkdf2(inputPassword, salt,
            IterationCount, HashAlgorithmName.SHA512, HashSize);

        if (!hashedInput.SequenceEqual(hash))
        {
            return Error.Problem(IPasswordHasherExtensions.IncorrectPasswordCode,
                IPasswordHasherExtensions.IncorrectPasswordDetail);
        }

        return Result.Succeed();
    }
}
