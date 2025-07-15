using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EnumAttribute : Attribute
{
    public string ReferenceName { get; }

    public EnumAttribute(string referenceName) =>
        ReferenceName = referenceName;
}
