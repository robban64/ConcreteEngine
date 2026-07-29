using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Engine.Render;


public sealed class RenderRegistry
{
    public static ShaderId DepthShader;
    public static ShaderId CompositeShader;
    public static ShaderId ColorFilterShader;
    public static ShaderId PresentShader;
    public static ShaderId HighlightShader;
    public static ShaderId BoundingBoxShader;

    private readonly GfxFrameBuffers _gfxFbo;

    private int _fboCount;
    private readonly RenderFbo[] _fboRegistry = new RenderFbo[RenderLimits.FboSlots];

    private ReadOnlySpan<RenderFbo> GetFrameBuffers() => _fboRegistry.AsSpan(0, _fboCount);

    internal RenderRegistry(GfxContext gfx)
    {
        _gfxFbo = gfx.FrameBuffers;
        RegisterUbo(gfx.Buffers);
        RegisterFbo();
        _fboRegistry.AsSpan(0, _fboCount).Sort();
    }


    public bool TryGetRenderFbo(FboKey key, out RenderFbo fbo)
    {
        var keyIndex = key.Index();
        if ((uint)keyIndex >= (uint)_fboRegistry.Length || _fboRegistry[keyIndex].Key != key)
            return (fbo = GetByKey(key)!) != null;

        fbo = _fboRegistry[keyIndex];
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderFbo? GetByKey(FboKey key)
    {
        foreach (var fb in GetFrameBuffers())
        {
            if (fb.Key == key) return fb;
        }

        return null;
    }

    public void RecreateFixedFrameBuffer<TTarget>(FboVariant variant, Size2D size)
        where TTarget : unmanaged, IRenderTarget
    {
        if (variant < 0 || variant > RenderLimits.MaxFboVariants) Throwers.InvalidArgument(nameof(variant));

        var fbo = GetByKey(new FboKey (TargetRegistry<TTarget>.TagIndex, variant));
        if (fbo == null) Throwers.NotFoundBy(nameof(variant), variant.Value);

        var meta = GfxRegistry.GetMeta(fbo.FboId);
        if (meta.Size == size) return;

        ValidateOutputSize(size, fbo.IsShadowFbo);
        ArgumentOutOfRangeException.ThrowIfEqual(size, meta.Size);
        ArgumentOutOfRangeException.ThrowIfEqual(fbo.IsFixedSize, false);

        try
        {
            _gfxFbo.RecreateFrameBuffer(fbo.FboId, size);
        }
        catch (Exception ex) when (Utils.ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate fbo({variant}): {ex.Message}", ex);
        }

        if (fbo.IsShadowFbo)
            RenderContext.DepthTexture = GfxRegistry.GetMeta(fbo.FboId).Attachments.DepthTexture;
    }


    public void RecreateScreenDependentFbo(Size2D outputSize)
    {
        ValidateOutputSize(outputSize, false);

        try
        {
            foreach (var fbo in GetFrameBuffers())
            {
                if (fbo.IsFixedSize) continue;
                _gfxFbo.RecreateFrameBuffer(fbo.FboId, fbo.CalculateSize(outputSize));
            }
        }
        catch (Exception ex) when (Utils.ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate screen fbo: {ex.Message}", ex);
        }
    }


    internal void Register<TTarget>(FboVariant variant, CreateFboInfo entry, FboResizeMode resizeMode = FboResizeMode.Screen, Func<Size2D, Size2D>? calc = null)
        where TTarget : unmanaged, IRenderTarget
    {
        var isShadowFbo = typeof(TTarget) == typeof(ShadowTarget);
        ValidateOutputSize(entry.Size, isShadowFbo);
        if (_fboCount > RenderLimits.FboSlots || _fboRegistry[_fboCount] != null!)
            Throwers.InvalidOperation(nameof(_fboCount));

        var fboId = _gfxFbo.CreateFrameBuffer(entry);
        var renderFbo = new RenderFbo(fboId, TargetRegistry<TTarget>.FboKey(variant), resizeMode, calc);
        if (isShadowFbo)
        {
            if (resizeMode != FboResizeMode.Fixed)
                Throwers.InvalidArgument("Shadow map require fixed size policy");

            renderFbo.IsShadowFbo = true;
            RenderContext.DepthTexture = GfxRegistry.GetMeta(fboId).Attachments.DepthTexture;
        }

        _fboRegistry[_fboCount++] = renderFbo;
    }

    private static void ValidateOutputSize(Size2D outputSize, bool isShadowMap)
    {
        if (outputSize < RenderLimits.MinOutputSize) Throwers.InvalidArgument(nameof(outputSize));
        if (isShadowMap)
        {
            if (outputSize > RenderLimits.MaxShadowMapSize) Throwers.InvalidArgument(nameof(outputSize));
            if (outputSize < RenderLimits.MinShadowMapSize) Throwers.InvalidArgument(nameof(outputSize));
        }
        else if (outputSize > RenderLimits.MaxOutputSize)
        {
            Throwers.InvalidArgument(nameof(outputSize));
        }
    }

    private static void RegisterFbo()
    {
        TargetRegistry<ShadowTarget>.RegisterTag();
        TargetRegistry<SceneTarget>.RegisterTag();
        TargetRegistry<LightTarget>.RegisterTag();
        TargetRegistry<PostFxTarget>.RegisterTag();
        TargetRegistry<OutputTarget>.RegisterTag();
    }

    private static void RegisterUbo(GfxBuffers gfxBuffers)
    {
        EngineUniformRecord.UboId = gfxBuffers.CreateUniformBuffer<EngineUniformRecord>();
        FrameUniform.UboId = gfxBuffers.CreateUniformBuffer<FrameUniform>();
        CameraUniform.UboId = gfxBuffers.CreateUniformBuffer<CameraUniform>();
        DirectionalLightUniform.UboId = gfxBuffers.CreateUniformBuffer<DirectionalLightUniform>();
        LightUniform.UboId = gfxBuffers.CreateUniformBuffer<LightUniform>();
        ShadowUniform.UboId = gfxBuffers.CreateUniformBuffer<ShadowUniform>();
        MaterialUniform.UboId = gfxBuffers.CreateUniformBuffer<MaterialUniform>();
        DrawObjectUniform.UboId = gfxBuffers.CreateUniformBuffer<DrawObjectUniform>();
        DrawAnimationUniform.UboId = gfxBuffers.CreateUniformBuffer<DrawAnimationUniform>();
        PostFxUniform.UboId = gfxBuffers.CreateUniformBuffer<PostFxUniform>();
        EditorEffectsUniform.UboId = gfxBuffers.CreateUniformBuffer<EditorEffectsUniform>();
    }


    //
    private static byte _targetCounter;

    public static class TargetRegistry<TTarget> where TTarget : unmanaged, IRenderTarget
    {
        private static bool _isBound;
        public static byte TagIndex { get; private set; }

        private static InlineArray4<byte> _passIds;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FboKey FboKey(FboVariant variant) => new(TagIndex, variant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PassTargetKey PassKey(FboVariant variant) =>
            new(TagIndex, variant, new PassId(_passIds[variant]));

        public static PassTargetKey BindPassTarget(FboVariant variant, PassId passId)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(variant.Value, RenderLimits.MaxFboVariants);

            if (!_isBound) throw new InvalidOperationException($"PassTag not registered. {typeof(TTarget).Name}");

            if (_passIds[variant] != 0) throw new InvalidOperationException(nameof(variant));

            _passIds[variant] = passId.Value;
            return PassKey(variant);
        }

        public static void RegisterTag()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_targetCounter, RenderLimits.FboSlots);

            if (_isBound)
                throw new InvalidOperationException($"PassTag already registered. {typeof(TTarget).Name}");

            TagIndex = _targetCounter++;
            _isBound = true;
        }
    }
}