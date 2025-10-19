#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

internal sealed class RenderFboRegistry
{
    private readonly GfxFrameBuffers _gfxFbo;
    private readonly GfxResourceApi _gfxApi;

    private int _fboCount = 0;

    private readonly RenderFbo[] _fboRegistry = new RenderFbo[RenderLimits.FboSlots];
    public ReadOnlySpan<RenderFbo> FrameBuffers => _fboRegistry.AsSpan(0, _fboCount);


    public RenderFboRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceContext.ResourceManager.GetGfxApi();
        _gfxFbo = gfx.FrameBuffers;

        _gfxApi.BindMetaChanged<FrameBufferId, FrameBufferMeta>(OnFboChange);
    }

    public void BeginRegistration()
    {
        RegisterTag<ShadowPassTag>();
        RegisterTag<ScenePassTag>();
        RegisterTag<LightPassTag>();
        RegisterTag<PostPassTag>();
        RegisterTag<ScreenPassTag>();
    }

    public void FinishRegistration()
    {
        _fboRegistry.AsSpan(0, _fboCount).Sort(RenderFbo.FboKeyComparer.Instance);
    }

    public void RegisterTag<TTag>() where TTag : unmanaged, IRenderPassTag
        => TagRegistry.RegisterTag<TTag>();

    public void Register<TTag>(FboVariant variant, RegisterFboEntry entry, Size2D outputSize)
        where TTag : unmanaged, IRenderPassTag
    {
        InvalidOpThrower.ThrowIf(_fboCount >= RenderLimits.FboSlots);
        InvalidOpThrower.ThrowIfNotNull(_fboRegistry[_fboCount]);

        var gfxDescriptor = entry.Build(outputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);
        var meta = _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>(fboId);

        var key = TagRegistry.FboKey<TTag>(variant);
        var sizePolicy = entry.FboSizePolicy ?? RenderFbo.SizePolicy.Default();

        var renderFbo = new RenderFbo(fboId, key, 0, sizePolicy);
        renderFbo.UpdateFromMeta(in meta);

        _fboRegistry[_fboCount++] = renderFbo;
    }

    internal void RecreateSizedFrameBuffer(Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.Height, 1, nameof(outputSize));

        var fbos = FrameBuffers;
        Span<(FrameBufferId, Size2D)> newSizes = stackalloc (FrameBufferId, Size2D)[fbos.Length];
        var idx = 0;
        foreach (var fbo in fbos)
        {
            if (fbo.IsFixedSize) continue;
            newSizes[idx++] = (fbo.FboId, fbo.CalculateNewSize(outputSize));
        }

        _gfxFbo.RecreateSizedFrameBuffer(newSizes.Slice(0, idx));
    }


    public RenderFbo? GetRenderFbo(FboTagKey key)
    {
        var span = _fboRegistry.AsSpan(0, _fboCount);
        foreach (var fb in span)
        {
            if (fb.TagKey == key) return fb;
        }

        return null;
    }

    public bool TryGetRenderFbo(FboTagKey key, out RenderFbo fbo)
    {
        var span = _fboRegistry.AsSpan(0, _fboCount);
        foreach (var fb in span)
        {
            if (fb.TagKey != key) continue;
            fbo = fb;
            return true;
        }

        fbo = null!;
        return false;
    }

    private void OnFboChange(FrameBufferId id, in GfxMetaChanged<FrameBufferMeta> message)
    {
        RenderFbo? renderFbo = null;
        foreach (var fb in FrameBuffers)
        {
            if (fb.FboId != id) continue;
            renderFbo = fb;
        }

        InvalidOpThrower.ThrowIfNull(renderFbo, nameof(id));
        renderFbo!.UpdateFromMeta(in message.NewMeta);
    }
}