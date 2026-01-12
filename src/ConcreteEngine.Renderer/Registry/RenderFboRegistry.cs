using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Utility;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderFboRegistry
{
    private readonly GfxFrameBuffers _gfxFbo;
    private readonly GfxResourceApi _gfxApi;

    private int _fboCount;

    private readonly RenderFbo[] _fboRegistry = new RenderFbo[RenderLimits.FboSlots];

    private ReadOnlySpan<RenderFbo> GetFrameBuffers() => _fboRegistry.AsSpan(0, _fboCount);

    internal Size2D ShadowMapSize;
    internal Size2D OutputSize;

    internal void OnFboChange(int id)
    {
        var fboId = (FrameBufferId)id;
        var meta = _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>(fboId);

        var renderFbo = GetRenderFboById(fboId);
        if (renderFbo is null) ThrowNotFound(fboId);

        renderFbo.UpdateFromMeta(in meta);
    }

    internal RenderFboRegistry(GfxContext gfx)
    {
        _gfxFbo = gfx.FrameBuffers;
        _gfxApi = gfx.ResourceManager.GetGfxApi();
    }

    internal void BeginRegistration(Size2D outputSize)
    {
        OutputSize = outputSize;

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
        if (typeof(TTag) == typeof(ShadowPassTag))
        {
            if (entry.FboSizePolicy!.Mode != FboResizeMode.Fixed)
                throw new ArgumentException("Shadow map require fixed size policy");

            renderFbo.HasShadowMap = true;
            ShadowMapSize = entry.FboSizePolicy!.GetFixedSize();
        }

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
        foreach (var fb in GetFrameBuffers())
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
        foreach (var fb in GetFrameBuffers())
        {
            if (fb.TagKey == key) return fb;
        }

        return null;
    }

    public RenderFbo? GetRenderFboById(FrameBufferId id)
    {
        foreach (var fb in GetFrameBuffers())
        {
            if (fb.FboId == id) return fb;
        }

        return null;
    }

    public void RecreateFixedFrameBuffer<TTag>(FboVariant variant, Size2D outputSize) where TTag : class
    {
        if (variant < 0 || variant > RenderLimits.MaxFboVariants)
            throw new ArgumentOutOfRangeException(nameof(variant));

        ValidateOutputSize(outputSize, typeof(TTag) == typeof(ShadowPassTag));

        var key = TagRegistry.FboKey<TTag>(variant);
        var fbo = GetRenderFbo(key);

        if (fbo == null) ThrowNotFound(key);
        ArgumentOutOfRangeException.ThrowIfEqual(outputSize, fbo.Size);
        InvalidOpThrower.ThrowIfNot(fbo.IsFixedSize, nameof(fbo.IsFixedSize));

        fbo.ChangeSizePolicy(RenderFboSizePolicy.Fixed(outputSize));

        try
        {
            _gfxFbo.RecreateFrameBuffer(fbo.FboId, outputSize);
            if (fbo.HasShadowMap) ShadowMapSize = outputSize;
        }
        catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate fbo({variant}): {ex.Message}", ex);
        }
    }


    public void RecreateScreenDependentFbo(Size2D outputSize)
    {
        ValidateOutputSize(outputSize, false);

        var fboSpan = GetFrameBuffers();
        Span<(FrameBufferId, Size2D)> newSizes = stackalloc (FrameBufferId, Size2D)[fboSpan.Length];

        var idx = 0;
        foreach (var fbo in fboSpan)
        {
            if (fbo.IsFixedSize) continue;
            newSizes[idx++] = (fbo.FboId, fbo.CalculateNewSize(outputSize));
        }


        try
        {
            _gfxFbo.RecreateSizedFrameBuffer(newSizes.Slice(0, idx));
            OutputSize = outputSize;
        }
        catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate screen fbo: {ex.Message}", ex);
        }
    }


    internal void DrainFboIds(FboResizeMode mode, Action<ReadOnlySpan<FrameBufferId>> pendingIds)
    {
        Span<FrameBufferId> newSizes = stackalloc FrameBufferId[GetFrameBuffers().Length];
        var idx = 0;
        foreach (var fbo in GetFrameBuffers())
        {
            if (fbo.SizePolicy.Mode != mode) continue;
            newSizes[idx++] = fbo.FboId;
        }

        pendingIds(newSizes);
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