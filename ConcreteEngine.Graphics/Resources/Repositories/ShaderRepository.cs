#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public interface IShaderRepository
{
    UboArena GetOrCreateUboArena(UniformGpuSlot slot);
    UniformBufferId GetUboId(UniformGpuSlot slot);
    ShaderLayout GetShaderLayout(ShaderId shaderId);
}

//TODO refactor UboArena out?
internal sealed class ShaderRepository : IShaderRepository
{
    private readonly Dictionary<ShaderId, ShaderLayout> _shaderLayouts = new(8);
    private readonly Dictionary<UniformGpuSlot, UboArena> _uboArenas = new();
    private readonly UniformBufferId[] _uboRegistry;

    private readonly FrontendStoreHub _resources;

    internal ShaderRepository(FrontendStoreHub resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        _resources = resources;
        _uboRegistry = new  UniformBufferId[GraphicsEnumCache.ShaderBufferUniformVals.Length];
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniformBufferId GetUboId(UniformGpuSlot slot) => _uboRegistry[(int)slot];

    public ShaderLayout GetShaderLayout(ShaderId shaderId)
    {
        shaderId.IsValidOrThrow();
        var hasResource = _shaderLayouts.TryGetValue(shaderId, out var uniformTable);
        if (!hasResource || uniformTable == null) GraphicsException.ThrowResourceNotFound(shaderId.Value);
        return uniformTable;
    }


    public UboArena GetOrCreateUboArena(UniformGpuSlot slot)
    {
        if (!_uboArenas.TryGetValue(slot, out var uboArena))
        {
            var meta = _resources.UboStore.GetMeta(_uboRegistry[(int)slot]);
            _uboArenas[slot] = uboArena = new UboArena(in meta);
        }

        return uboArena;
    }
    
    internal void AddUboToSlot(UniformGpuSlot slot, UniformBufferId uboId)
    {
        uboId.IsValidOrThrow();
        var id = _uboRegistry[(int)slot];
        if (id.IsValid())
            throw GraphicsException.ResourceAlreadyExists<UniformBufferId>(uboId);
        
        _uboRegistry[(int)slot] = uboId;
    }

    internal void Add(ShaderId shaderId, in ShaderMeta meta, List<(string, int)> uniforms)
    {
        _shaderLayouts.Add(shaderId, new ShaderLayout(uniforms, meta.Samplers));
    }
    
    internal bool Remove(ShaderId shaderId)
    {
        return _shaderLayouts.Remove(shaderId);
    }
}

public sealed class ShaderLayout
{
    private readonly int[] _locs;
    private readonly Dictionary<string, int> _rawUniforms;
    
    public int Samplers { get; }

    public ShaderLayout(List<(string, int)> uniformPairs, int samplers)
    {
        Samplers = samplers;
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