using ConcreteEngine.Core.Rendering.Utility;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;




public readonly record struct UboSlotKey<T>(UniformBufferId UboId, UboSlot Slot) where T : unmanaged, IUniformGpuData
{
    internal static UboSlotKey<T> Make(UniformBufferId uboId, int value) => new(uboId, new UboSlot(value));
}

public sealed class RenderUbo //where TUbo : unmanaged, IUniformGpuData
{
    public UniformBufferId Id { get; }
    public UboSlot Slot { get; }

    private UniformBufferMeta _metaCache;
    private UboArena? _uboBufferArena;

    public RenderUbo(UniformBufferId id, UboSlot slot, in UniformBufferMeta meta)
    {
        Id = id;
        Slot = slot;
        _metaCache = meta;
    }
    
    internal void UpdateMeta(in UniformBufferMeta meta) => _metaCache = meta;

    public ref readonly UniformBufferMeta RenderData() => ref _metaCache;

    public UboArena UboArena()
    {
        if (_uboBufferArena != null) return _uboBufferArena;
        _uboBufferArena = new UboArena(in _metaCache);
        return _uboBufferArena;
    }
}