#region

using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal sealed class UniformRegistry
{
    private readonly SortedList<int, UniformTable> _shaderUniforms = new(8);

    private readonly List<UniformBufferId>[] _uboSlots;
    private readonly Dictionary<UniformGpuSlot, UniformBufferId> _uboRegistry = new(4);

    public UniformRegistry()
    {
        _uboSlots = new List<UniformBufferId>[GraphicsEnumCache.ShaderBufferUniformVals.Length];
        for (int i = 0; i < _uboSlots.Length; i++)
            _uboSlots[i] = new List<UniformBufferId>(4);
    }

    
    public void AddUboToSlot(UniformGpuSlot slot, UniformBufferId uboId)
    {
        var list = _uboSlots[(int)slot];
        if (list.Contains(uboId))
            throw GraphicsException.ResourceAlreadyExists<UniformBufferId>(uboId);

        list.Add(uboId);
        _uboRegistry.Add(slot, uboId);

    }

    public void Add(ShaderId shaderId, UniformTable uniformTable)
    {
        _shaderUniforms.Add(shaderId.Id, uniformTable);
    }

    public UniformBufferId GetUboId(UniformGpuSlot slot) => _uboRegistry[slot];

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