using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EnumAttribute : Attribute
{
    public readonly string ReferenceName;

    public EnumAttribute(string referenceName) =>
        ReferenceName = referenceName;
}
