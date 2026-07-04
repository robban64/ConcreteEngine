namespace ConcreteEngine.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Enum)]
public sealed class EnumExtAttribute : Attribute
{
    public bool Index { get; init; }
    public bool Utf8 { get; init; }
    public EnumExtAttribute(){}
}
