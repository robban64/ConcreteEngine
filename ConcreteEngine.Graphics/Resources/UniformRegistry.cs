#region

using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal sealed class UniformRegistry
{
    private readonly SortedList<int, UniformTable> _shaderUniforms = new(8);

    private readonly List<UniformBufferId>[] _uboSlots;

    private readonly Dictionary<ShaderBufferUniform, Type> _typeRegistry = new(4);
    private readonly Dictionary<ShaderBufferUniform, UniformBufferId> _uboRegistry = new(4);

    public UniformRegistry()
    {
        RegisterUbo<FrameUniformGpuData>(ShaderBufferUniform.Frame);
        RegisterUbo<CameraUniformGpuData>(ShaderBufferUniform.Camera);
        RegisterUbo<DirLightUniformGpuData>(ShaderBufferUniform.DirLight);
        RegisterUbo<MaterialUniformGpuData>(ShaderBufferUniform.Material);
        RegisterUbo<DrawObjectUniformGpuData>(ShaderBufferUniform.DrawObject);

        _uboSlots = new List<UniformBufferId>[GraphicsEnumCache.ShaderBufferUniformVals.Length];
        for (int i = 0; i < _uboSlots.Length; i++)
            _uboSlots[i] = new List<UniformBufferId>(4);
    }

    public IReadOnlyList<UniformBufferId> GetUniformBuffersBySlot(ShaderBufferUniform slot) => _uboSlots[(int)slot];


    public void RegisterUbo<T>(ShaderBufferUniform uniform) where T : struct, IUniformGpuData
    {
        _typeRegistry.Add(uniform, typeof(T));
    }
    
    public void AddUboToSlot(ShaderBufferUniform slot, UniformBufferId uboId)
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

    public UniformBufferId GetUboId(ShaderBufferUniform slot) => _uboRegistry[slot];

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