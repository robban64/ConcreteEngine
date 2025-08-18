#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal sealed class UniformRegistry
{
    private readonly SortedList<int, UniformTable> _shaderUniforms = new(8);

    public void Add(ShaderId shaderId, UniformTable uniformTable)
    {
        _shaderUniforms.Add(shaderId.Id, uniformTable);
    }

    public UniformTable Get(ShaderId shaderId)
    {
        var hasResource = _shaderUniforms.TryGetValue(shaderId.Id, out var uniformTable);
        if (!hasResource || uniformTable == null) GraphicsException.ThrowResourceNotFound(shaderId.Id);
        return uniformTable;
    }

    public bool Remove(ShaderId shaderId)
    {
        return _shaderUniforms.Remove(shaderId.Id);
    }
}