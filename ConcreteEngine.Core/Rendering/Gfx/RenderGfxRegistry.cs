using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

internal sealed class RenderGfxRegistry
{
    private readonly List<RenderFbo> _fboRegistry = new();
    private readonly List<RenderShader> _shaderRegistry = new();

    private readonly FrozenTypeRegistry<IUniformGpuData, UboSlot> _uboTypeSlots = new();
    private readonly List<RenderUbo> _uboRegistry = new();
    
    private readonly GfxContext _gfx;

    public RenderGfxRegistry(GfxContext gfx)
    {
        var someFboId = new FrameBufferId(1);
        _gfx = gfx;
        var gfxResources = gfx.ResourceContext.ResourceManager;

    }

    public UboSlotRef<T> RegisterUniformBuffer<T>() where T : unmanaged, IUniformGpuData
    {
        var slotRef = UboSlotRef<T>.Make(_fboRegistry.Count);
        _gfx.Buffers.CreateUniformBuffer<FrameUniformGpuData>(slotRef.Slot.Value);
    }

}