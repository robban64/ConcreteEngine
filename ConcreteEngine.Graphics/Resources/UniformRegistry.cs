using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class UniformRegistry
{
    private readonly SortedList<ushort, UniformTable> _shaderUniforms = new(8);

    public void Add(ushort resourceId, UniformTable uniformTable)
    {
        _shaderUniforms.Add(resourceId, uniformTable);
    }
    
    public UniformTable Get(ushort resourceId)
    {
        var hasResource = _shaderUniforms.TryGetValue(resourceId, out var uniformTable);
        if (!hasResource || uniformTable == null) GraphicsException.ThrowResourceNotFound(resourceId);
        return uniformTable;
    }

    public bool Remove(ushort resourceId)
    {
        return _shaderUniforms.Remove(resourceId);
    }
    
}