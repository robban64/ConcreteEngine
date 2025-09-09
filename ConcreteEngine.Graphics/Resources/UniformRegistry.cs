#region

using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal sealed class UniformRegistry
{
    private readonly Dictionary<ShaderId, UniformTable> _shaderUniforms = new(8);

    private readonly Dictionary<UniformGpuSlot, UniformBufferId> _uboRegistry 
        = new(GraphicsEnumCache.ShaderBufferUniformVals.Length);

    
    public void AddUboToSlot(UniformGpuSlot slot, UniformBufferId uboId)
    {
        if (!_uboRegistry.TryAdd(slot, uboId))
            throw GraphicsException.ResourceAlreadyExists<UniformBufferId>(uboId);
    }

    public void Add(ShaderId shaderId, UniformTable uniformTable)
    {
        _shaderUniforms.Add(shaderId, uniformTable);
    }

    public UniformBufferId GetUboId(UniformGpuSlot slot) => _uboRegistry[slot];

    public UniformTable Get(ShaderId shaderId)
    {
        var hasResource = _shaderUniforms.TryGetValue(shaderId, out var uniformTable);
        if (!hasResource || uniformTable == null) GraphicsException.ThrowResourceNotFound(shaderId.Id);
        return uniformTable;
    }

    public bool Remove(ShaderId shaderId)
    {
        return _shaderUniforms.Remove(shaderId);
    }
}