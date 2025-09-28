using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

internal sealed class RenderRegistry
{
    private readonly record struct RegistrationData(bool Enabled, Size2D OutputSize);

    private readonly DictionaryTypeRegistry<IFboTag, List<RenderFbo>> _fboRegistry = new();
    private readonly DictionaryTypeRegistry<IUniformGpuData, RenderUbo> _uboRegistry = new();
    private readonly DictionaryRegistry<ShaderId, RenderShader> _shaderRegistry = new();


    private readonly GfxContext _gfx;
    private readonly GfxResourceApi _gfxApi;

    private RegistrationData _registrationData;
    
    public RenderRegistry(GfxContext gfx)
    {
        _gfx = gfx;
        _gfxApi = gfx.ResourceContext.ResourceManager.GetGfxApi();
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
        _fboRegistry.Freeze();
        _uboRegistry.Freeze();
        _shaderRegistry.Freeze();
    }

    public void RegisterShader(ShaderId shaderId)
    {
        _gfx.Shaders.GetUniformList(shaderId, out _, out var uniforms);
        _shaderRegistry.Register(shaderId, new RenderShader(shaderId, uniforms));
    }
    
    public void RegisterFrameBuffer<TTag>(RegisterFboEntry entry) where TTag : unmanaged, IFboTag
    {
        InvalidOpThrower.ThrowIfNot(_registrationData.Enabled);
        var gfxDescriptor = entry.ToGfxDescriptor(_registrationData.OutputSize);
        var fboId = _gfx.FrameBuffers.CreateFrameBuffer(gfxDescriptor);
        var renderFbo = new RenderFbo(fboId, _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>);
        
        if(entry.CalculateSizeDel != null && entry.CalculateRatio is { } ratio)
            renderFbo.UseCalculatedSize(entry.CalculateSizeDel, ratio);
        else if(entry.FixedSize is { } fixedSize)
            renderFbo.UseFixedSize(fixedSize);

        if (!_fboRegistry.TryGet<IFboTag>(out var list))
            _fboRegistry.Register<TTag>(list = []);
        
        list.Add(renderFbo);
    }

    public void RegisterUniformBuffer<T>(int slot) where T : unmanaged, IUniformGpuData
    {
        var slotRef = UniformBufferTagRegistry<T>.MakeSlot(slot);
        InvalidOpThrower.ThrowIfNot(slot == slotRef.Slot.Value);
        var uboId = _gfx.Buffers.CreateUniformBuffer<FrameUniformGpuData>(slot);
        var renderUbo = new RenderUbo(uboId, slotRef.Slot, _gfxApi.GetMeta<UniformBufferId, UniformBufferMeta>);
        _uboRegistry.Register<T>(renderUbo);
    }
    
    private static class UniformBufferTagRegistry<IUbo> where IUbo : unmanaged, IUniformGpuData
    {
        public static int Slot { get; set; }
        public static UboSlotRef<IUbo> MakeSlot(int slot) => UboSlotRef<IUbo>.Make(Slot = slot);
    }

}