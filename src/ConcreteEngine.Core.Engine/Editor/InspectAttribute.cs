namespace ConcreteEngine.Core.Engine.Editor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class InspectAttribute : Attribute
{
    public string? DisplayName { get; init; } 
}

/*
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InputNumberAttribute(Type? valueType = null) : Attribute
{
    public Type? ValueType { get; } = valueType;
    public FieldKind Kind { get; set; } = FieldKind.Input;
    public string? Format { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
    public float Speed { get; set; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InputColorAttribute(bool hasAlpha = true) : Attribute
{
    public bool HasAlpha { get; } = hasAlpha;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InputComboAttribute : Attribute
{
    public string? Placeholder { get; set; }
    public int StartAt { get; set; }
}
*/
