using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
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
    private readonly RenderFbo[] _frameBuffers;
    private readonly InlineArray4<byte>[] _slotByTagIndex;
    
    internal RenderRegistry(GfxContext gfx)
    {
        _gfxFbo = gfx.FrameBuffers;
        _frameBuffers = new RenderFbo[RenderLimits.FboSlots];
        _slotByTagIndex = new InlineArray4<byte>[RenderLimits.FboSlots];
        for (int i = 0; i < RenderLimits.FboSlots; i++)
        {
            for (int j = 0; j < 4; j++) _slotByTagIndex[i][j] = byte.MaxValue;
        }

        RegisterUbo(gfx.Buffers);
        RegisterFbo();
    }
    
    private ReadOnlySpan<RenderFbo> GetFrameBuffers() => new(_frameBuffers, 0, _fboCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRenderFbo(FboKey key, [NotNullWhen(true)] out RenderFbo? fbo)
    {
        var index = _slotByTagIndex[key.TagIndex][key.Variant];
        if (index < byte.MaxValue)
        {
            fbo = _frameBuffers[index];
            return true;
        }

        fbo = null;
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderFbo GetByKey(FboKey key)
    {
        var index = _slotByTagIndex[key.TagIndex][key.Variant];
        return _frameBuffers[index];
    }

    public void RecreateFixedFrameBuffer<TTarget>(FboVariant variant, Size2D size)
        where TTarget : unmanaged, IRenderTarget
    {
        RecreateFixedFrameBuffer(TargetRegistry<TTarget>.TagIndex, variant, size);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecreateFixedFrameBuffer(byte tagIndex, FboVariant variant, Size2D size)
    {
        if (variant < 0 || variant > RenderLimits.MaxFboVariants) Throwers.InvalidArgument(nameof(variant));

        var fbo = GetByKey(new FboKey(tagIndex, variant));
        if (fbo == null!) Throwers.NotFoundBy(nameof(variant), variant.Value);

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


    internal void Register<TTarget>(FboVariant variant,
        CreateFboInfo entry,
        FboResizeMode resizeMode = FboResizeMode.Screen,
        Func<Size2D, Size2D>? calc = null)
        where TTarget : unmanaged, IRenderTarget
    {
        Register(TargetRegistry<TTarget>.FboKey(variant), TTarget.TargetKind, in entry, resizeMode, calc);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Register(FboKey key, RenderTargetKind targetKind, in CreateFboInfo entry,
        FboResizeMode resizeMode, Func<Size2D, Size2D>? calc)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Variant, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.TagIndex, 16);

        if (_fboCount > RenderLimits.FboSlots || _frameBuffers[_fboCount] != null!)
            Throwers.InvalidOperation(nameof(_fboCount));

        if (_slotByTagIndex[key.TagIndex][key.Variant] != byte.MaxValue)
            Throwers.InvalidOperation(nameof(_slotByTagIndex));

        ValidateOutputSize(entry.Size, targetKind == RenderTargetKind.Shadow);

        var fboId = _gfxFbo.CreateFrameBuffer(entry);
        var renderFbo = new RenderFbo(fboId, key, targetKind, resizeMode, calc);
        if (targetKind == RenderTargetKind.Shadow)
            RenderContext.DepthTexture = GfxRegistry.GetMeta(fboId).Attachments.DepthTexture;

        _slotByTagIndex[key.TagIndex][key.Variant] = (byte)_fboCount;
        _frameBuffers[_fboCount++] = renderFbo;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
        TransformUniform.UboId = gfxBuffers.CreateUniformBuffer<TransformUniform>();
        SkinningUniform.UboId = gfxBuffers.CreateUniformBuffer<SkinningUniform>();
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
            if (!_isBound) Throwers.NotFound(nameof(TTarget), "PassTag not registered.");
            if (_passIds[variant] != 0) Throwers.InvalidArgument(nameof(variant));
            _passIds[variant] = passId.Value;
            return PassKey(variant);
        }

        public static void RegisterTag()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_targetCounter, RenderLimits.FboSlots);
            if (_isBound) Throwers.InvalidOperation("PassTag already registered.");
            TagIndex = _targetCounter++;
            _isBound = true;
        }
    }
}