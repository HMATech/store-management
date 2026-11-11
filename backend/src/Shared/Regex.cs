using System.Text.RegularExpressions;

namespace Shared;

public static partial class PasswordRegex
{
    private const string PasswordOneUppercaseRequirementRegexString = "(?=.*?[A-Z])";
    private const string PasswordOneLowercaseRequirementRegexString = "(?=.*?[a-z])";
    private const string PasswordOneNumberRequirementRegexString = "(?=.*?[0-9])";
    private const string PasswordOneSpecialCharacterRequirementRegexString = "(?=.*?[#?!@$%^&*-])";

    [GeneratedRegex(PasswordOneUppercaseRequirementRegexString)]
    public static partial Regex PasswordOneUppercaseRequirementRegex();

    [GeneratedRegex(PasswordOneLowercaseRequirementRegexString)]
    public static partial Regex PasswordOneLowercaseRequirementRegex();

    [GeneratedRegex(PasswordOneNumberRequirementRegexString)]
    public static partial Regex PasswordOneNumberRequirementRegex();

    [GeneratedRegex(PasswordOneSpecialCharacterRequirementRegexString)]
    public static partial Regex PasswordOneSpecialCharacterRequirementRegex();
}