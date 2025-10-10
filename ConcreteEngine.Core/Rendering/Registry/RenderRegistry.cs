#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

internal sealed class RenderRegistry
{
    private readonly record struct RegistrationData(bool Enabled, Size2D OutputSize);

    private readonly List<RenderFbo> _fboRegistry = new(8);
    private readonly RenderUbo[] _uboRegistry = new RenderUbo[RenderLimits.UboSlots];
    private RenderShader[] _shaderRegistry = Array.Empty<RenderShader>();

    private readonly GfxResourceApi _gfxApi;
    private readonly GfxFrameBuffers _gfxFbo;
    private readonly GfxBuffers _gfxBuffers;
    private readonly GfxShaders _gfxShaders;

    private RegistrationData _registrationData;

    internal IReadOnlyList<RenderFbo> RenderFbos => _fboRegistry;

    public RenderRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceContext.ResourceManager.GetGfxApi();
        _gfxFbo = gfx.FrameBuffers;
        _gfxBuffers = gfx.Buffers;
        _gfxShaders = gfx.Shaders;

        _gfxApi.BindMetaChanged<FrameBufferId, FrameBufferMeta>(OnFboChange);
        _gfxApi.BindMetaChanged<UniformBufferId, UniformBufferMeta>(OnUboChange);
    }

    public void BeginRegistration(Size2D outputSize)
    {
        InvalidOpThrower.ThrowIf(_registrationData.Enabled);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        _registrationData = new RegistrationData(true, outputSize);

        RenderStaticSetup.RegisterPassTagTypes();
    }

    public void FinishRegistration()
    {
        _fboRegistry.Sort();
        _registrationData = new RegistrationData(false, Size2D.Zero);
    }

    public void RegisterShaderCollection(IReadOnlyList<Shader> shaders)
    {
        _shaderRegistry = new RenderShader[shaders.Count];
        foreach (var shader in shaders)
        {
            var shaderId = shader.ResourceId;
            var uniforms = _gfxShaders.GetUniformList(shaderId);
            if (_shaderRegistry[shaderId - 1] != null) throw new InvalidOperationException(nameof(_shaderRegistry));
            _shaderRegistry[shaderId - 1] = new RenderShader(shaderId, uniforms);
        }
    }

    public void RegisterFrameBuffer<TTag>(FboVariant variant, RegisterFboEntry entry)
        where TTag : unmanaged, IRenderPassTag
    {
        InvalidOpThrower.ThrowIfNot(_registrationData.Enabled);

        var gfxDescriptor = entry.Build(_registrationData.OutputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);
        var meta = _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>(fboId);

        var key = TagRegistry.FboKey<TTag>(variant);
        var sizePolicy = entry.FboSizePolicy ?? RenderFbo.SizePolicy.Default();

        var renderFbo = new RenderFbo(fboId, key, 0, sizePolicy);
        renderFbo.UpdateFromMeta(in meta);
        _fboRegistry.Add(renderFbo);
    }

    public void RegisterUniformBuffer<TUbo>() where TUbo : unmanaged, IStd140Uniform
    {
        InvalidOpThrower.ThrowIfCapacityExceed(_uboRegistry, RenderLimits.UboSlots);
        if (!UniformBufferUtils.IsStd140Aligned<TUbo>())
            throw new InvalidOperationException($"{typeof(TUbo).Name} is not std140-aligned.");

        var newSlot = TagRegistry.RegisterUniformBufferSlot<TUbo>();
        var uboId = _gfxBuffers.CreateUniformBuffer<TUbo>(newSlot);
        var meta = _gfxApi.GetMeta<UniformBufferId, UniformBufferMeta>(uboId);

        _uboRegistry[newSlot] = new RenderUbo(uboId, newSlot, in meta);
    }

    public bool TryGetRenderFbo<TTag>(FboVariant fboVariant, out RenderFbo fbo)
        where TTag : unmanaged, IRenderPassTag
    {
        var key = TagRegistry.FboKey<TTag>(fboVariant);
        return TryGetRenderFbo(key, out fbo);
    }

    public bool TryGetRenderFbo(FboTagKey key, out RenderFbo fbo)
    {
        foreach (var fb in _fboRegistry)
        {
            if (fb.TagKey != key) continue;
            fbo = fb;
            return true;
        }

        fbo = null!;
        return false;
    }

    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry[shaderId - 1];

    public RenderUbo GetRenderUbo<TUbo>() where TUbo : unmanaged, IStd140Uniform
    {
        var slot = TagRegistry.UniformBufferSlot<TUbo>();
        return _uboRegistry[slot];
    }

    private void OnFboChange(FrameBufferId id, in GfxMetaChanged<FrameBufferMeta> message)
    {
        var idx = id - 1;
        InvalidOpThrower.ThrowIf(idx >= _fboRegistry.Count, nameof(id));

        var renderFbo = _fboRegistry[idx];
        Debug.Assert(renderFbo != null, $"Sync error missing fbo : {id}");
        renderFbo.UpdateFromMeta(in message.NewMeta);
    }

    private void OnUboChange(UniformBufferId id, in GfxMetaChanged<UniformBufferMeta> message)
    {
        var meta = message.NewMeta;
        var renderUbo = _uboRegistry[meta.Slot];
        renderUbo.SetCapacity(meta.Capacity);
    }
}