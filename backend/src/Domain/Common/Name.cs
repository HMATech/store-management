namespace Domain.Common;

public class Name
{
    public string Value { get; private set; } = string.Empty;
    public string Normalized { get; private set; } = string.Empty;

    private Name() { }

    public static Name CreateNew(string name)
    {
        return new Name
        {
            Value = name,
            Normalized = name.ToLower().Trim()
        };
    }
}