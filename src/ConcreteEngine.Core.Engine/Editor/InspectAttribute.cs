namespace ConcreteEngine.Core.Engine.Editor;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectAttribute(string? displayName = null) : Attribute
{
    public string? DisplayName { get; } = displayName;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InputFieldAttribute(Type? valueType = null) : Attribute
{
    public Type? ValueType { get; } = valueType;
    public FieldKind Kind { get; set; } = FieldKind.Input;
    public string? Format { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
    public float Speed { get; set; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ColorFieldAttribute(bool hasAlpha = true) : Attribute
{
    public bool HasAlpha { get; } = hasAlpha;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ComboFieldAttribute : Attribute
{
    public string? Placeholder { get; set; }
    public int StartAt { get; set; }
}

