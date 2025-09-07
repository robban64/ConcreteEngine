#region

using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal sealed class UniformRegistry
{
    private readonly SortedList<int, UniformTable> _shaderUniforms = new(8);

    private readonly List<UniformBufferId>[] _uboSlots;

    private readonly Dictionary<UniformGpuData, Type> _typeRegistry = new(4);
    private readonly Dictionary<UniformGpuData, UniformBufferId> _uboRegistry = new(4);

    public UniformRegistry()
    {
        RegisterUbo<FrameUniformGpuData>(UniformGpuData.Frame);
        RegisterUbo<CameraUniformGpuData>(UniformGpuData.Camera);
        RegisterUbo<DirLightUniformGpuData>(UniformGpuData.DirLight);
        RegisterUbo<MaterialUniformGpuData>(UniformGpuData.Material);
        RegisterUbo<DrawObjectUniformGpuData>(UniformGpuData.DrawObject);

        _uboSlots = new List<UniformBufferId>[GraphicsEnumCache.ShaderBufferUniformVals.Length];
        for (int i = 0; i < _uboSlots.Length; i++)
            _uboSlots[i] = new List<UniformBufferId>(4);
    }

    public IReadOnlyList<UniformBufferId> GetUniformBuffersBySlot(UniformGpuData slot) => _uboSlots[(int)slot];


    public void RegisterUbo<T>(UniformGpuData uniformGpuData) where T : struct, IUniformGpuData
    {
        _typeRegistry.Add(uniformGpuData, typeof(T));
    }
    
    public void AddUboToSlot(UniformGpuData slot, UniformBufferId uboId)
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

    public UniformBufferId GetUboId(UniformGpuData slot) => _uboRegistry[slot];

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