namespace Domain.Common;

// Use this attribute for DateTime properties that are not in UTC
[AttributeUsage(AttributeTargets.Property)]
public class NotUtcAttribute : Attribute { }