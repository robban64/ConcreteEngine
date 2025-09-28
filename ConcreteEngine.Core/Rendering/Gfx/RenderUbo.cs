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


    private UboArena? _uboBufferArena;

    public RenderUbo(UniformBufferId id, UboSlot slot)
    {
        Id = id;
        Slot = slot;
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