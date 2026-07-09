namespace ConcreteEngine.Core.Engine.Editor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class InspectAttribute : Attribute
{
    public string ObjectName { get; init; }
    public string DisplayName { get; init; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class InspectIncludeAttribute : Attribute
{
    public string? AccessSuffix { get; init; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public abstract class InspectInputAttribute(string? label = null) : Attribute
{
    public Type? Converter { get; init; }
}


public sealed class InputNumberAttribute(string? label = null) : InspectInputAttribute(label)
{
    public InputStyle Style { get; init; } = InputStyle.Input;
    public string? Format { get; init; } = "%.2f";
    public float Min { get; init; }
    public float Max { get; init; }
    public float Speed { get; init; } = 1;
}

public sealed class InputColorAttribute(string? label = null) : InspectInputAttribute(label)
{
    public bool HasAlpha { get; init; } = true;
}


public sealed class InputComboAttribute(string? label = null) : InspectInputAttribute(label)
{
    public string? Placeholder { get; init; }
    public int StartAt { get; init; }
}
