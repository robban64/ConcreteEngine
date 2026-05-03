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
// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderFboRegistry
{
    private readonly GfxFrameBuffers _gfxFbo;
    private readonly GfxResourceApi _gfxApi;

    private int _fboCount;

    internal Size2D ShadowMapSize;
    internal Size2D OutputSize;

    private readonly RenderFbo[] _fboRegistry = new RenderFbo[RenderLimits.FboSlots];

    private ReadOnlySpan<RenderFbo> GetFrameBuffers() => _fboRegistry.AsSpan(0, _fboCount);

    internal void OnFboChange(int id)
    {
        var fboId = (FrameBufferId)id;
        var meta = _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>(fboId);

        var renderFbo = GetById(fboId);
        if (renderFbo is null) throw new InvalidOperationException($"FrameBuffer with id: {fboId} not found");

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

        PassTags<ShadowPassTag>.RegisterTag();
        PassTags<ScenePassTag>.RegisterTag();
        PassTags<LightPassTag>.RegisterTag();
        PassTags<PostPassTag>.RegisterTag();
        PassTags<ScreenPassTag>.RegisterTag();
    }


    internal void Register<TTag>(FboVariant variant, RegisterFboEntry entry, Size2D outputSize) where TTag : class
    {
        ArgOutOfRangeThrower.ThrowIfSizeTooSmall(outputSize, new Size2D(RenderLimits.MinOutputSize));
        ArgOutOfRangeThrower.ThrowIfSizeTooBig(outputSize, new Size2D(RenderLimits.MaxOutputSize));

        InvalidOpThrower.ThrowIf(_fboCount >= RenderLimits.FboSlots);
        InvalidOpThrower.ThrowIfNotNull(_fboRegistry[_fboCount]);

        var gfxDescriptor = entry.Build(outputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);
        var meta = _gfxApi.GetMeta<FrameBufferId, FrameBufferMeta>(fboId);

        var sizePolicy = entry.FboSizePolicy ?? RenderFboSizePolicy.Default();

        var renderFbo = new RenderFbo(fboId, PassTags<TTag>.FboKey(variant), 0, sizePolicy);
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


    public bool TryGetRenderFbo(FboTagKey key, out RenderFbo fbo)
    {
        var keyIndex = key.Index();
        if ((uint)keyIndex >= (uint)_fboRegistry.Length || _fboRegistry[keyIndex].TagKey != key)
            return (fbo = GetByKey(key)!) != null;

        fbo = _fboRegistry[keyIndex];
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderFbo? GetByKey(FboTagKey key)
    {
        foreach (var fb in GetFrameBuffers())
        {
            if (fb.TagKey == key) return fb;
        }

        return null;
    }

    public RenderFbo? GetById(FrameBufferId id)
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

        var key = PassTags<TTag>.FboKey(variant);
        var fbo = GetByKey(key);

        if (fbo == null) throw new InvalidOperationException($"FrameBuffer with key: {key} not found");
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
    
    //
    private static int _passTagCounter;

    public static class PassTags<TTag> where TTag : class
    {
        private static int _tagIndex = -1;
        private static readonly PassId[] PassIds = new PassId[RenderLimits.MaxFboVariants];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PassTagKey PassKey(FboVariant variant)  => new(_tagIndex, variant, PassIds[variant]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FboTagKey FboKey(FboVariant variant)  => new(_tagIndex, variant);
        
        public static PassTagKey BindFboPassId(FboVariant variant, PassId passId)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(variant.Value, RenderLimits.MaxFboVariants);

            if (_tagIndex < 0)
                throw new InvalidOperationException($"PassTag not registered. {typeof(TTag).Name}");

            if (PassIds[variant] != default) throw new InvalidOperationException(nameof(variant));

            PassIds[variant] = passId;
            return PassKey(variant);
        }

        public static void RegisterTag()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_passTagCounter, RenderLimits.FboSlots);

            if (_tagIndex >= 0)
                throw new InvalidOperationException($"PassTag already registered. {typeof(TTag).Name}");

            _tagIndex = _passTagCounter++;
        }
    }
}