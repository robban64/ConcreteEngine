namespace ConcreteEngine.Core.Engine.Editor;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class)]
public class InspectableAttribute : Attribute
{
    public string? Format;

    public InspectableAttribute(string? format = null)
    {
        Format = format;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct)]
public class InspectablePrimitiveAttribute : Attribute
{

    public InspectablePrimitiveAttribute()
    {}
}
