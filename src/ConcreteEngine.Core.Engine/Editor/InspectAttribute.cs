namespace ConcreteEngine.Core.Engine.Editor;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InspectAttribute : Attribute
{ 
    public string DisplayName { get; init; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class InspectIncludeAttribute : Attribute
{
    public string? AccessSuffix { get; init; } 
}


//
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public abstract class InspectInputAttribute : Attribute
{
    public string? Segment { get; init; }
    public string? DisplayName { get; init; }
}


public sealed class InputNumberAttribute(InputStyle style = InputStyle.Input) : InspectInputAttribute
{
    public InputStyle Style { get; } = style;
    public Type? Converter { get; init; }
    public string? Format { get; init; }
    public float Min { get; init; }
    public float Max { get; init; }
    public float Speed { get; init; }
}

public sealed class InputColorAttribute : InspectInputAttribute
{
    public bool HasAlpha { get; init; }
}


public sealed class InputComboAttribute(int[]? values = null, string[]? names = null) : InspectInputAttribute
{
    public string[]? Names { get; } = names;
    public int[]? Values { get; } = values;
    
    public bool UseEnumExt { get; init; }

    public string? Placeholder { get; init; }
    public int StartAt { get; init; }
}

