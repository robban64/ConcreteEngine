using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

internal sealed class RenderRegistry
{
    private const int MaxUboSlots = 32;
    private readonly record struct RegistrationData(bool Enabled, Size2D OutputSize);

    private readonly DictionaryTypeRegistry<IFboTag, List<RenderFbo>> _fboRegistry = new();
   // private readonly DictionaryTypeRegistry<IUniformGpuData, RenderUbo> _uboRegistry = new();
    private readonly DictionaryRegistry<ShaderId, RenderShader> _shaderRegistry = new();

    private UboSlot _nextSlot = new(0);
    private RenderUbo[] _ubos = Array.Empty<RenderUbo>();
    private List<RenderUbo> _uboRegistry = new(8);

    private readonly GfxResourceApi _gfxApi;
    private readonly GfxFrameBuffers _gfxFbo;
    private readonly GfxBuffers _gfxBuffers;
    private readonly GfxShaders _gfxShaders;
    

    private RegistrationData _registrationData;

    public RenderRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceContext.ResourceManager.GetGfxApi();
        _gfxFbo = gfx.FrameBuffers;
        _gfxBuffers = gfx.Buffers;
        _gfxShaders = gfx.Shaders;
    }

    public RenderFbo GetRenderFbo<TTag>(int index) where TTag : unmanaged, IFboTag
    {
        return _fboRegistry.GetRequired<TTag>()[index];
    }
    
    public RenderUbo GetRenderUbo<TUbo>() where TUbo : unmanaged, IUniformGpuData
    {
        var slot = UniformBufferTagRegistry<TUbo>.Slot.Value;
        if (slot < 0 || slot >= _nextSlot.Value)
            throw new InvalidOperationException($"{typeof(TUbo).Name} UBO not registered.");
        return _ubos[slot];
    }
    
    public RenderShader GetRenderShader(ShaderId shaderId) 
    {
        return _shaderRegistry.GetRequired(shaderId);
    }

    public void BeginRegistration(Size2D outputSize)
    {
        InvalidOpThrower.ThrowIf(_registrationData.Enabled);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        _registrationData = new RegistrationData(true, outputSize);
    }

    public void FinishRegistration()
    {
        _ubos = _uboRegistry.ToArray();
        _uboRegistry.Clear();
        _uboRegistry = null!;
        
        _fboRegistry.Freeze((registry) =>
        {
            foreach (var kv in registry)
            {
                kv.Value.Sort();
            }
        });
        _shaderRegistry.Freeze();
    }

    public void RegisterShaders(ShaderId shaderId)
    {
        var uniforms = _gfxShaders.GetUniformList(shaderId);
        _shaderRegistry.Register(shaderId, new RenderShader(shaderId, uniforms));
    }

    public void RegisterFrameBuffer<TTag>(RegisterFboEntry entry) where TTag : unmanaged, IFboTag
    {
        InvalidOpThrower.ThrowIfNot(_registrationData.Enabled);
        var gfxDescriptor = entry.ToGfxDescriptor(_registrationData.OutputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);
        var renderFbo = new RenderFbo(fboId, _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>);

        if (!_fboRegistry.TryGet<IFboTag>(out var list))
            _fboRegistry.Register<TTag>(list = new List<RenderFbo>());

        list.Add(renderFbo);
    }

    public void RegisterUniformBuffer<T>() where T : unmanaged, IUniformGpuData
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_nextSlot, MaxUboSlots, nameof(_nextSlot));
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw new InvalidOperationException($"{typeof(T).Name} is not std140-aligned.");

        if (UniformBufferTagRegistry<T>.Slot.Value >= 0)
            throw new InvalidOperationException($"{typeof(T).Name} UBO already registered at slot {UniformBufferTagRegistry<T>.Slot.Value}.");

        
        var slot = _nextSlot;
        var uboId = _gfxBuffers.CreateUniformBuffer<T>(slot);
        _uboRegistry.Add(new RenderUbo(uboId, slot));
        UniformBufferTagRegistry<T>.Slot = slot;

        _nextSlot = new UboSlot(slot.Value + 1);
    }

    
    private static class UniformBufferTagRegistry<IUbo> where IUbo : unmanaged, IUniformGpuData
    {
        public static UboSlot Slot { get; set; } = new (-1);
    }

}