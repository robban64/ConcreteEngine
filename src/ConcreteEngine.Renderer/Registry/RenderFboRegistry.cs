using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Renderer.Configuration;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Utility;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderFboRegistry
{
    private readonly GfxFrameBuffers _gfxFbo;

    private int _fboCount;

    internal Size2D ShadowMapSize;
    internal Size2D OutputSize;

    private readonly RenderFbo[] _fboRegistry = new RenderFbo[RenderLimits.FboSlots];

    private ReadOnlySpan<RenderFbo> GetFrameBuffers() => _fboRegistry.AsSpan(0, _fboCount);

    internal void OnFboChange(int id)
    {
        var fboId = (FrameBufferId)id;
        var meta = GfxResourceApi.GetMeta(fboId);

        var renderFbo = GetById(fboId);
        if (renderFbo is null) Throwers.NotFoundBy(nameof(FrameBufferId), id);

        renderFbo.UpdateFromMeta(in meta);
    }

    internal RenderFboRegistry(GfxContext gfx)
    {
        _gfxFbo = gfx.FrameBuffers;
    }

    internal void BeginRegistration(Size2D outputSize)
    {
        OutputSize = outputSize;

        PassTags<ShadowPassTag>.RegisterTag();
        PassTags<ScenePassTag>.RegisterTag();
        PassTags<LightPassTag>.RegisterTag();
        PassTags<PostPassTag>.RegisterTag();
        PassTags<OutputPassTag>.RegisterTag();
        //PassTags<ScreenPassTag>.RegisterTag();
    }


    internal void Register<TTag>(FboVariant variant, RegisterFboEntry entry, Size2D outputSize) where TTag : class
    {
        if(outputSize < RenderLimits.MinOutputSize) Throwers.InvalidArgument(nameof(outputSize));
        if(outputSize > RenderLimits.MaxOutputSize) Throwers.InvalidArgument(nameof(outputSize));

        if(_fboCount > RenderLimits.FboSlots) Throwers.InvalidOperation(nameof(_fboCount));
        if(_fboRegistry[_fboCount] != null!) Throwers.InvalidOperation(nameof(RenderFboRegistry));

        var gfxDescriptor = entry.Build(outputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);
        var meta = GfxResourceApi.GetMeta(fboId);

        var sizePolicy = entry.FboSizePolicy ?? RenderFboSizePolicy.MakeDefault();

        var renderFbo = new RenderFbo(fboId, PassTags<TTag>.FboKey(variant), 0, sizePolicy);
        if (typeof(TTag) == typeof(ShadowPassTag))
        {
            if (entry.FboSizePolicy!.Mode != FboResizeMode.Fixed)
                Throwers.InvalidArgument("Shadow map require fixed size policy");

            renderFbo.HasShadowMap = true;
            ShadowMapSize = entry.FboSizePolicy!.FixedSize;
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
        if (variant < 0 || variant > RenderLimits.MaxFboVariants) Throwers.InvalidArgument(nameof(variant));

        ValidateOutputSize(outputSize, typeof(TTag) == typeof(ShadowPassTag));

        var key = PassTags<TTag>.FboKey(variant);
        var fbo = GetByKey(key);

        if (fbo == null) Throwers.NotFoundBy(nameof(variant),variant.Value);
        ArgumentOutOfRangeException.ThrowIfEqual(outputSize, fbo.Size);
        ArgumentOutOfRangeException.ThrowIfEqual(fbo.IsFixedSize, false);

        fbo.ChangeSizePolicy(RenderFboSizePolicy.MakeFixed(outputSize));

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

        try
        {
            foreach (var fbo in GetFrameBuffers())
            {
                if (fbo.IsFixedSize) continue;
                _gfxFbo.RecreateFrameBuffer(fbo.FboId, fbo.CalculateNewSize(outputSize));
            }

            OutputSize = outputSize;
        }
        catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate screen fbo: {ex.Message}", ex);
        }
    }

    private static void ValidateOutputSize(Size2D outputSize, bool isShadowMap)
    {
        if(outputSize < RenderLimits.MinOutputSize) Throwers.InvalidArgument(nameof(outputSize));
        if (isShadowMap)
        {
            if(outputSize > RenderLimits.MaxShadowMapSize) Throwers.InvalidArgument(nameof(outputSize));
            if(outputSize < RenderLimits.MinShadowMapSize) Throwers.InvalidArgument(nameof(outputSize));
        }
        else if( outputSize > RenderLimits.MaxOutputSize)
        {
            Throwers.InvalidArgument(nameof(outputSize));
        }
    }

    //
    private static byte _passTagCounter;

    public static class PassTags<TTag> where TTag : class
    {
        private static bool _isBound;
        private static byte _tagIndex;
        private static PassIdVariants _passIds;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe PassTagKey PassKey(FboVariant variant) =>
            new(_tagIndex, variant, new PassId(_passIds.Value[variant]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FboTagKey FboKey(FboVariant variant) => new(_tagIndex, variant);

        public static unsafe PassTagKey BindFboPassId(FboVariant variant, PassId passId)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(variant.Value, RenderLimits.MaxFboVariants);

            if (!_isBound) throw new InvalidOperationException($"PassTag not registered. {typeof(TTag).Name}");

            if (_passIds.Value[variant] != 0) throw new InvalidOperationException(nameof(variant));

            _passIds.Value[variant] = passId.Value;
            return PassKey(variant);
        }

        public static void RegisterTag()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_passTagCounter, RenderLimits.FboSlots);

            if (_isBound)
                throw new InvalidOperationException($"PassTag already registered. {typeof(TTag).Name}");

            _tagIndex = _passTagCounter++;
            _isBound = true;
        }
    }

    private unsafe struct PassIdVariants
    {
        public fixed byte Value[RenderLimits.MaxFboVariants];
    }
}