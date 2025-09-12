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

    private readonly IGfxResourceManager _resources;

    internal ShaderRegistry(IGfxResourceManager resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        _resources = resources;
        _uboRegistry = new  UniformBufferId[GraphicsEnumCache.ShaderBufferUniformVals.Length];
    }

    public UboArena GetOrCreateUboArena(UniformGpuSlot slot)
    {
        if (!_uboArenas.TryGetValue(slot, out var _uboArena))
        {
            var meta = _resources.UboStore.GetMeta(_uboRegistry[(int)slot]);
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



public sealed class ShaderLayout
{
    private readonly int[] _locs;
    private readonly Dictionary<string, int> _rawUniforms;

    public ShaderLayout(List<(string, int)> uniformPairs)
    {
        _locs = new int [GraphicsEnumCache.ShaderUniformVals.Length];
        _rawUniforms = new Dictionary<string, int>(uniformPairs.Count);


        foreach (var (uniform, location) in uniformPairs)
        {
            _rawUniforms.Add(uniform, location);
            var idx = uniform.IndexOf(".", StringComparison.Ordinal);
            if (idx <= 0) continue;
            
            var uniformName = uniform.AsSpan(0, idx);
        }
            
        for (int i = 0; i < _locs.Length; i++)
        {
            var uniformName = GraphicsEnumCache.ShaderUniformVals[i].ToUniformName();
            if (_rawUniforms.TryGetValue(uniformName, out var uniformLocation))
            {
                _locs[i] = uniformLocation;
                continue;
            }

            _locs[i] = -1;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ShaderUniform uniform) => _locs[(int)uniform] >= 0;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetUniformLocation(string key, int defaultValue = -1)
    {
        return _rawUniforms.TryGetValue(key, out var uniformLocation) ? uniformLocation : defaultValue;
    }

    public int this[ShaderUniform uniform]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locs[(int)uniform];
    }
}