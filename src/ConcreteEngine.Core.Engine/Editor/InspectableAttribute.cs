namespace ConcreteEngine.Core.Engine.Editor;

public enum InspectorFieldKind : byte
{
    None,
    Id,
    Name,
    Generation
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class)]
public class InspectableAttribute : Attribute
{
    public string? Format;
    public InspectorFieldKind FieldKind;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct)]
public class InspectablePrimitiveAttribute : Attribute
{
    public InspectorFieldKind FieldKind;
}