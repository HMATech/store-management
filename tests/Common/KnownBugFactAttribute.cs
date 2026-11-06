namespace Tests.Common;

/// <summary>
/// Marks a test that asserts the correct behavior of code that currently has a bug. The test is skipped so the suite
/// stays green. Set RUN_KNOWN_BUGS=1 to run it. Once the bug is fixed, change the attribute to a plain [Fact].
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class KnownBugFactAttribute : FactAttribute
{
    public KnownBugFactAttribute(string reason)
    {
        if (Environment.GetEnvironmentVariable("RUN_KNOWN_BUGS") != "1")
        {
            Skip = $"Known bug: {reason}";
        }
    }
}
