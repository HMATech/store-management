namespace Shared.Tests;

public class PasswordRegexTests
{
    [Theory]
    [InlineData("abcD", true)]
    [InlineData("abcd", false)]
    public void Uppercase_RequiresOneUppercaseCharacter(string input, bool expected)
    {
        Assert.Equal(expected, PasswordRegex.PasswordOneUppercaseRequirementRegex().IsMatch(input));
    }

    [Theory]
    [InlineData("ABCd", true)]
    [InlineData("ABCD", false)]
    public void Lowercase_RequiresOneLowercaseCharacter(string input, bool expected)
    {
        Assert.Equal(expected, PasswordRegex.PasswordOneLowercaseRequirementRegex().IsMatch(input));
    }

    [Theory]
    [InlineData("abc1", true)]
    [InlineData("abcd", false)]
    public void Number_RequiresOneDigit(string input, bool expected)
    {
        Assert.Equal(expected, PasswordRegex.PasswordOneNumberRequirementRegex().IsMatch(input));
    }

    [Theory]
    [InlineData("abc#", true)]
    [InlineData("abc?", true)]
    [InlineData("abc!", true)]
    [InlineData("abc@", true)]
    [InlineData("abc$", true)]
    [InlineData("abc%", true)]
    [InlineData("abc^", true)]
    [InlineData("abc&", true)]
    [InlineData("abc*", true)]
    [InlineData("abc-", true)]
    [InlineData("abc_", false)]
    [InlineData("abcd", false)]
    public void SpecialCharacter_RequiresOneFromAllowedSet(string input, bool expected)
    {
        Assert.Equal(expected, PasswordRegex.PasswordOneSpecialCharacterRequirementRegex().IsMatch(input));
    }
}
