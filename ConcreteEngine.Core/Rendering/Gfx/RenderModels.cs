using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Utility;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Core.Rendering.Gfx;


public readonly record struct UboSlot(int Value);

public readonly record struct UboSlotRef<T>(UboSlot Slot) where T : unmanaged, IUniformGpuData
{
    internal static UboSlotRef<T> Make(int value) => new(new UboSlot(value));
}

public sealed class RenderUbo where TUbo : unmanaged, IUniformGpuData
{
    public UniformBufferId Id { get; }
    public UboSlot Slot { get; }


    private UboArena? _uboBufferArena;
    private readonly GetMetaDel<UniformBufferId, UniformBufferMeta> _getMetaDel;

    public RenderUbo(UniformBufferId id, UboSlot slot, GetMetaDel<UniformBufferId, UniformBufferMeta> getMetaDel)
    {
        Id = id;
        Slot = slot;
        _getMetaDel = getMetaDel;
    }

    
    public ref readonly UniformBufferMeta RenderData()
    {
        return ref _getMetaDel(Id);
    }

    public UboArena UboArena()
    {
        if (_uboBufferArena != null) return _uboBufferArena;
        ref readonly var uboMeta  = ref _getMetaDel(Id);
        _uboBufferArena = new UboArena(in uboMeta);

        return _uboBufferArena;
    }
}

public sealed class RenderShader
{
    public ShaderId Id { get;  }
    private readonly int[] _locations;
    private readonly Dictionary<string, int> _uniforms;

    public IReadOnlyDictionary<string, int> Uniforms => _uniforms;

    public RenderShader(ShaderId id, List<(string, int)> uniformPairs)
    {
        Id = id;
        _locations = new int [GraphicsEnumCache.ShaderUniformVals.Length];
        _uniforms = new Dictionary<string, int>(uniformPairs.Count);

        foreach (var (uniform, location) in uniformPairs)
        {
            _uniforms.Add(uniform, location);
            var idx = uniform.IndexOf(".", StringComparison.Ordinal);
        }

        for (int i = 0; i < _locations.Length; i++)
        {
            var uniformName = GraphicsEnumCache.ShaderUniformVals[i].ToUniformName();
            if (_uniforms.TryGetValue(uniformName, out var uniformLocation))
            {
                _locations[i] = uniformLocation;
                continue;
            }

            _locations[i] = -1;
        }
    }

    public int this[ShaderUniform uniform]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locations[(int)uniform];
    }
}