namespace ConcreteEngine.Core.Assets.IO;

internal sealed class ShaderBuilder
{
    private Dictionary<(IncludeKind, string), string> _includes;

    public bool TryGetInclude(IncludeKind includeKind, string name, out string? value)
        => _includes.TryGetValue((includeKind, name), out value);

    public enum IncludeKind
    {
        Ubo,
        Struct,
        Utility
    }
}