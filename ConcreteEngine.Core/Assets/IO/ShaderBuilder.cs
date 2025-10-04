namespace ConcreteEngine.Core.Assets.IO;

internal sealed class ShaderBuilder
{
    public enum ImportKind
    {
        Ubo,
        Struct,
        Utility
    }
    
    private readonly Dictionary<(ImportKind, string), string> _imports = new (16);

    public bool TryGetInclude(ImportKind importKind, string name, out string? value)
        => _imports.TryGetValue((importKind, name), out value);


    private void LoadIncludes()
    {
        
    }
    
    
}