namespace Tools.DebugInterface;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class DebugCommandAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}