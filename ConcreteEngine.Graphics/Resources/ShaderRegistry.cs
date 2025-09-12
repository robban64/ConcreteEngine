#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public interface IShaderRegistry
{
    UboArena GetOrCreateUboArena(UniformGpuSlot slot);
    UniformBufferId GetUboId(UniformGpuSlot slot);
    ShaderLayout GetShaderLayout(ShaderId shaderId);
}
internal sealed class ShaderRegistry : IShaderRegistry
{
    private readonly Dictionary<ShaderId, ShaderLayout> _shaderLayouts = new(8);
    private readonly Dictionary<UniformGpuSlot, UboArena> _uboArenas = new();
    
    private readonly UniformBufferId[] _uboRegistry;

    private readonly IReadResourceStore<UniformBufferId, UniformBufferMeta> _uboStore;

    internal ShaderRegistry(IReadResourceStore<UniformBufferId, UniformBufferMeta> uboStore)
    {
        ArgumentNullException.ThrowIfNull(uboStore);
        _uboStore = uboStore;
        _uboRegistry = new  UniformBufferId[GraphicsEnumCache.ShaderBufferUniformVals.Length];
    }

    public UboArena GetOrCreateUboArena(UniformGpuSlot slot)
    {
        if (!_uboArenas.TryGetValue(slot, out var _uboArena))
        {
            var meta = _uboStore.GetMeta(_uboRegistry[(int)slot]);
            _uboArenas[slot] = _uboArena = new UboArena(in meta);
        }

        return _uboArena;
    }
    
    public void AddUboToSlot(UniformGpuSlot slot, UniformBufferId uboId)
    {
        var id = _uboRegistry[(int)slot];
        if (id.IsValid())
            throw GraphicsException.ResourceAlreadyExists<UniformBufferId>(uboId);
        
        _uboRegistry[(int)slot] = uboId;
    }

    public void Add(ShaderId shaderId, ShaderLayout shaderLayout)
    {
        _shaderLayouts.Add(shaderId, shaderLayout);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniformBufferId GetUboId(UniformGpuSlot slot) => _uboRegistry[(int)slot];

    public ShaderLayout GetShaderLayout(ShaderId shaderId)
    {
        var hasResource = _shaderLayouts.TryGetValue(shaderId, out var uniformTable);
        if (!hasResource || uniformTable == null) GraphicsException.ThrowResourceNotFound(shaderId.Id);
        return uniformTable;
    }

    public bool Remove(ShaderId shaderId)
    {
        return _shaderLayouts.Remove(shaderId);
    }
}