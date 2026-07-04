namespace ConcreteEngine.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Enum)]
public sealed class EnumExtAttribute : Attribute
{
    public bool ToIndex { get; init; }
    public bool ToUtf8 { get; init; }

    public EnumExtAttribute(){}
}
