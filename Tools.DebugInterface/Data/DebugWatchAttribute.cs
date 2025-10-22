namespace Tools.DebugInterface.Data;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DebugWatchAttribute : Attribute
{
    public string? Name { get; init; }
    public bool ReadOnly { get; init; }
}