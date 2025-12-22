using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Utility;

namespace ConcreteEngine.Renderer.Registry;

public interface IRenderFboRegistry
{
    void RecreateFixedFrameBuffer<TTag>(FboVariant variant, Size2D outputSize)
        where TTag : class;

    void RecreateScreenDependentFbo(Size2D outputSize);
}

internal sealed class RenderFboRegistry : IRenderFboRegistry
{
    private readonly GfxFrameBuffers _gfxFbo;
    private readonly GfxResourceApi _gfxApi;

    private int _fboCount = 0;

    private readonly RenderFbo[] _fboRegistry = new RenderFbo[RenderLimits.FboSlots];

    private ReadOnlySpan<RenderFbo> FrameBufferSpan => _fboRegistry.AsSpan(0, _fboCount);


    internal RenderFboRegistry(GfxContext gfx)
    {
        _gfxFbo = gfx.FrameBuffers;
        _gfxApi = gfx.ResourceManager.GetGfxApi();
    }

    internal void BeginRegistration()
    {
        RegisterTag<ShadowPassTag>();
        RegisterTag<ScenePassTag>();
        RegisterTag<LightPassTag>();
        RegisterTag<PostPassTag>();
        RegisterTag<ScreenPassTag>();
    }

    internal void RegisterTag<TTag>() where TTag : class => TagRegistry.RegisterTag<TTag>();

    internal void Register<TTag>(FboVariant variant, RegisterFboEntry entry, Size2D outputSize) where TTag : class
    {
        ArgOutOfRangeThrower.ThrowIfSizeTooSmall(outputSize, new Size2D(RenderLimits.MinOutputSize));
        ArgOutOfRangeThrower.ThrowIfSizeTooBig(outputSize, new Size2D(RenderLimits.MaxOutputSize));

        InvalidOpThrower.ThrowIf(_fboCount >= RenderLimits.FboSlots);
        InvalidOpThrower.ThrowIfNotNull(_fboRegistry[_fboCount]);

        var gfxDescriptor = entry.Build(outputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);
        var meta = _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>(fboId);

        var key = TagRegistry.FboKey<TTag>(variant);
        var sizePolicy = entry.FboSizePolicy ?? RenderFboSizePolicy.Default();

        var renderFbo = new RenderFbo(fboId, key, 0, sizePolicy);
        renderFbo.UpdateFromMeta(in meta);

        _fboRegistry[_fboCount++] = renderFbo;
    }


    internal void FinishRegistration()
    {
        _fboRegistry.AsSpan(0, _fboCount).Sort(RenderFbo.FboKeyComparer.Instance);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRenderFbo(FboTagKey key, out RenderFbo fbo)
    {
        foreach (var fb in FrameBufferSpan)
        {
            if (fb.TagKey != key) continue;
            fbo = fb;
            return true;
        }

        fbo = null!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderFbo? GetRenderFbo(FboTagKey key)
    {
        foreach (var fb in FrameBufferSpan)
        {
            if (fb.TagKey == key) return fb;
        }

        return null;
    }

    public RenderFbo? GetRenderFboById(FrameBufferId id)
    {
        foreach (var fb in FrameBufferSpan)
        {
            if (fb.FboId == id) return fb;
        }

        return null;
    }

    public void DrainFboIds(FboResizeMode mode, Action<ReadOnlySpan<FrameBufferId>> pendingIds)
    {
        Span<FrameBufferId> newSizes = stackalloc FrameBufferId[FrameBufferSpan.Length];
        var idx = 0;
        foreach (var fbo in FrameBufferSpan)
        {
            if (fbo.SizePolicy.Mode != mode) continue;
            newSizes[idx++] = fbo.FboId;
        }

        pendingIds(newSizes);
    }

    public void RecreateFixedFrameBuffer<TTag>(FboVariant variant, Size2D outputSize) where TTag : class
    {
        if (variant < 0 || variant > RenderLimits.MaxFboVariants)
            throw new ArgumentOutOfRangeException(nameof(variant));

        ValidateOutputSize(outputSize, typeof(TTag) == typeof(ShadowPassTag));

        var key = TagRegistry.FboKey<TTag>(variant);
        var fbo = GetRenderFbo(key);
        if (fbo == null) ThrowNotFound(key);
        InvalidOpThrower.ThrowIfNot(fbo.IsFixedSize, nameof(fbo.IsFixedSize));
        InvalidOpThrower.ThrowIf(fbo.Size == outputSize, nameof(outputSize));
        fbo.ChangeSizePolicy(RenderFboSizePolicy.Fixed(outputSize));

        try
        {
            _gfxFbo.RecreateFrameBuffer(fbo.FboId, outputSize);
        }
        catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate fbo({variant}): {ex.Message}", ex);
        }
    }


    public void RecreateScreenDependentFbo(Size2D outputSize)
    {
        ValidateOutputSize(outputSize, false);
        var fbos = FrameBufferSpan;
        Span<(FrameBufferId, Size2D)> newSizes = stackalloc (FrameBufferId, Size2D)[fbos.Length];
        var idx = 0;
        foreach (var fbo in fbos)
        {
            if (fbo.IsFixedSize) continue;
            newSizes[idx++] = (fbo.FboId, fbo.CalculateNewSize(outputSize));
        }

        try
        {
            _gfxFbo.RecreateSizedFrameBuffer(newSizes.Slice(0, idx));
        }
        catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate screen fbo: {ex.Message}", ex);
        }
    }


    private static void ValidateOutputSize(Size2D outputSize, bool isShadowMap)
    {
        ArgOutOfRangeThrower.ThrowIfSizeTooSmall(outputSize, new Size2D(RenderLimits.MinOutputSize));
        if (isShadowMap)
        {
            ArgOutOfRangeThrower.ThrowIfSizeTooBig(outputSize, new Size2D(RenderLimits.MaxShadowMapSize));
            ArgOutOfRangeThrower.ThrowIfSizeTooSmall(outputSize, new Size2D(RenderLimits.MinShadowMapSize));
        }
        else
        {
            ArgOutOfRangeThrower.ThrowIfSizeTooBig(outputSize, new Size2D(RenderLimits.MaxOutputSize));
        }
    }

    [DoesNotReturn]
    [StackTraceHidden]
    internal static void ThrowNotFound(FrameBufferId id) =>
        throw new InvalidOperationException($"FrameBuffer with id: {id} not found");

    [DoesNotReturn]
    [StackTraceHidden]
    internal static void ThrowNotFound(FboTagKey key) =>
        throw new InvalidOperationException($"FrameBuffer with key: {key} not found");
}