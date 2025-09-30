using System.Runtime.CompilerServices;
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

    private readonly DictionaryRegistry<FboTagKey, RenderFbo> _fboRegistry = new();

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

        _gfxApi.BindMetaChanged<FrameBufferId, FrameBufferMeta>(OnFboChange);
    }

    public RenderFbo GetRenderFbo<TTag, TSlot>(int version) 
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
        => _fboRegistry.GetRequired(FboTagKey.Make<TTag, TSlot>(version));

    
    public bool TryGetRenderFbo<TTag, TSlot>(int version, out RenderFbo fbo)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        if (_fboRegistry.TryGet(FboTagKey.Make<TTag, TSlot>(version), out fbo))
            return true;

        fbo = null!;
        return false;
    }
    
    public bool TryGetRenderFbo(FboTagKey key, out RenderFbo fbo)
    {
        if (_fboRegistry.TryGet(key, out fbo))
            return true;

        fbo = null!;
        return false;
    }


    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry.GetRequired(shaderId);

    public RenderUbo GetRenderUbo<TUbo>() where TUbo : unmanaged, IUniformGpuData
    {
        var slot = RTypeRegistry.UniformBufferTag<TUbo>.Slot.Value;
        if (slot < 0 || slot >= _nextSlot.Value)
            throw new InvalidOperationException($"{typeof(TUbo).Name} UBO not registered.");
        return _ubos[slot];
    }

    public void BeginRegistration(Size2D outputSize)
    {
        InvalidOpThrower.ThrowIf(_registrationData.Enabled);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        _registrationData = new RegistrationData(true, outputSize);
        
        RTypeRegistry.RenderPassTag<ScenePassTag>.Register();
        RTypeRegistry.RenderPassTag<ShadowPassTag>.Register();
        RTypeRegistry.RenderPassTag<LightPassTag>.Register();
        RTypeRegistry.RenderPassTag<PostPassTag>.Register();
        RTypeRegistry.RenderPassTag<ScreenPassTag>.Register();
        
        RTypeRegistry.RenderPassSlot<ScenePassDrawSlot>.Register();
        RTypeRegistry.RenderPassSlot<ScenePassResolveSlot>.Register();
        
        RTypeRegistry.RenderPassSlot<PostPassASlot>.Register();
        RTypeRegistry.RenderPassSlot<PostPassBSlot>.Register();
        
        RTypeRegistry.RenderPassSlot<ScreenPassPresentSlot>.Register();

    }

    public void RegisterShader(ShaderId shaderId)
    {
        var uniforms = _gfxShaders.GetUniformList(shaderId);
        _shaderRegistry.Register(shaderId, new RenderShader(shaderId, uniforms));
    }

    public void RegisterFrameBuffer<TTag, TSlot>(int version, RegisterFboEntry entry)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        InvalidOpThrower.ThrowIfNot(_registrationData.Enabled);

        var gfxDescriptor = entry.ToGfxDescriptor(_registrationData.OutputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);
        var meta = _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>(fboId);

        var renderFbo = new RenderFbo(fboId, entry.FboSizePolicy);
        renderFbo.UpdateFromMeta(in meta);

        var key = FboTagKey.Make<TTag, TSlot>(version);
        _fboRegistry.Register(key, renderFbo);
    }

    public void RegisterUniformBuffer<T>() where T : unmanaged, IUniformGpuData
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_nextSlot, MaxUboSlots, nameof(_nextSlot));
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw new InvalidOperationException($"{typeof(T).Name} is not std140-aligned.");

        if (RTypeRegistry.UniformBufferTag<T>.Slot.Value >= 0)
            throw new InvalidOperationException(
                $"{typeof(T).Name} UBO already registered at slot {RTypeRegistry.UniformBufferTag<T>.Slot.Value}.");


        var slot = _nextSlot;
        var uboId = _gfxBuffers.CreateUniformBuffer<T>(slot);
        var meta = _gfxApi.GetMeta<UniformBufferId, UniformBufferMeta>(uboId);
        _uboRegistry.Add(new RenderUbo(uboId, slot, in meta));
        RTypeRegistry.UniformBufferTag<T>.Slot = slot;

        _nextSlot = new UboSlot(slot.Value + 1);
    }
    
    
    public void FinishRegistration()
    {
        _ubos = _uboRegistry.ToArray();
        _uboRegistry.Clear();
        _uboRegistry = null!;

        _fboRegistry.Freeze();
        _shaderRegistry.Freeze();
    }


    private void OnFboChange(FrameBufferId id, in GfxMetaChanged<FrameBufferMeta> message)
    {
    }
}