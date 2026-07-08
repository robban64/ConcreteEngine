namespace ConcreteEngine.Editor.Lib;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class EditorInspectorAttribute(Type targetType) : Attribute
{
    public Type TargetType { get; } = targetType;
    public string? DisplayName { get; init; }
}

