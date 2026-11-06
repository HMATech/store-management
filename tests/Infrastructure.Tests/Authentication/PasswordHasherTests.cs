using Application.Abstractions.Authentication;
using Infrastructure.Authentication;

namespace Infrastructure.Tests.Authentication;

public class PasswordHasherTests
{
    private readonly PasswordHasher hasher = new();

    [Fact]
    public void Hash_DoesNotContainPlainPassword()
    {
        var hash = hasher.Hash("Passw0rd!");

        Assert.DoesNotContain("Passw0rd!", hash);
        Assert.Equal(2, hash.Split(".+").Length);
    }

    [Fact]
    public void Hash_UsesDifferentSaltEachTime()
    {
        Assert.NotEqual(hasher.Hash("Passw0rd!"), hasher.Hash("Passw0rd!"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_WithMissingPassword_Throws(string? password)
    {
        Assert.ThrowsAny<ArgumentException>(() => hasher.Hash(password!));
    }

    [Fact]
    public void Verify_WithCorrectPassword_Succeeds()
    {
        Assert.True(hasher.Verify("Passw0rd!", hasher.Hash("Passw0rd!")).IsSuccess);
    }

    [Theory]
    [InlineData("passw0rd!")]
    [InlineData("Passw0rd! ")]
    [InlineData("Other")]
    public void Verify_WithWrongPassword_ReturnsIncorrectPassword(string input)
    {
        var result = hasher.Verify(input, hasher.Hash("Passw0rd!"));

        Assert.True(result.IsFailed);
        Assert.Equal(IPasswordHasherExtensions.IncorrectPasswordCode, result.Error.Code);
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("", "hash")]
    [InlineData("password", null)]
    [InlineData("password", "")]
    public void Verify_WithMissingInput_ReturnsInvalidInput(string? input, string? hash)
    {
        var result = hasher.Verify(input!, hash!);

        Assert.True(result.IsFailed);
        Assert.Equal(IPasswordHasherExtensions.InvalidInputCode, result.Error.Code);
    }

    [Theory]
    [InlineData("no-separator")]
    [InlineData("a.+b.+c")]
    public void Verify_WithMalformedHash_Throws(string hash)
    {
        Assert.Throws<InvalidOperationException>(() => hasher.Verify("password", hash));
    }

    [Fact]
    public void Verify_WithNonHexHash_Throws()
    {
        Assert.Throws<FormatException>(() => hasher.Verify("password", "zz.+zz"));
    }

    [Fact]
    public void Verify_WithTamperedHash_ReturnsIncorrectPassword()
    {
        var parts = hasher.Hash("Passw0rd!").Split(".+");
        var tampered = (parts[0][0] == 'A' ? "B" : "A") + parts[0][1..];

        var result = hasher.Verify("Passw0rd!", $"{tampered}.+{parts[1]}");

        Assert.Equal(IPasswordHasherExtensions.IncorrectPasswordCode, result.Error.Code);
    }

    [Fact]
    public void Verify_WithHashFromAnotherSalt_ReturnsIncorrectPassword()
    {
        var first = hasher.Hash("Passw0rd!").Split(".+");
        var second = hasher.Hash("Passw0rd!").Split(".+");

        var result = hasher.Verify("Passw0rd!", $"{first[0]}.+{second[1]}");

        Assert.Equal(IPasswordHasherExtensions.IncorrectPasswordCode, result.Error.Code);
    }
}
