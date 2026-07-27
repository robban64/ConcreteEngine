using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Configuration;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Utility;
// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderRegistry
{
    public static ShaderId DepthShader;
    public static ShaderId CompositeShader;
    public static ShaderId ColorFilterShader;
    public static ShaderId PresentShader;
    public static ShaderId HighlightShader;
    public static ShaderId BoundingBoxShader;

    private readonly GfxBuffers _gfxBuffers;
    private readonly GfxFrameBuffers _gfxFbo;

    private int _fboCount;
    private readonly RenderFbo[] _fboRegistry = new RenderFbo[RenderLimits.FboSlots];
    
    private ReadOnlySpan<RenderFbo> GetFrameBuffers() => _fboRegistry.AsSpan(0, _fboCount);

    internal RenderRegistry(GfxContext gfx)
    {
        _gfxFbo = gfx.FrameBuffers;
        _gfxBuffers = gfx.Buffers;
    }

    internal void BeginRegistration()
    {
        RegisterUbo(_gfxBuffers);
        RegisterFbo();
    }

    internal void FinishRegistration()
    {
        _fboRegistry.AsSpan(0, _fboCount).Sort();
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

    public void RecreateFixedFrameBuffer<TTarget>(FboVariant variant, Size2D outputSize) where TTarget : unmanaged, IRenderTarget
    {
        if (variant < 0 || variant > RenderLimits.MaxFboVariants) Throwers.InvalidArgument(nameof(variant));
        
        var key = TargetRegistry<TTarget>.FboKey(variant);
        var fbo = GetByKey(key);

        if (fbo == null) Throwers.NotFoundBy(nameof(variant), variant.Value);
        
        
        var meta = GfxRegistry.GetMeta(fbo.FboId);
        
        ValidateOutputSize(outputSize, fbo.IsShadowFbo);
        ArgumentOutOfRangeException.ThrowIfEqual(outputSize, meta.Size);
        ArgumentOutOfRangeException.ThrowIfEqual(fbo.IsFixedSize, false);

        fbo.ChangeSizePolicy(RenderFboSizePolicy.MakeFixed(outputSize));

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

        try
        {
            foreach (var fbo in GetFrameBuffers())
            {
                if (fbo.IsFixedSize) continue;
                _gfxFbo.RecreateFrameBuffer(fbo.FboId, fbo.CalculateNewSize(outputSize));
            }
        }
        catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
        {
            throw new GraphicsException($"Failed to recreate screen fbo: {ex.Message}", ex);
        }
    }
    
    
    internal void Register<TTarget>(FboVariant variant, RegisterFboEntry entry, Size2D outputSize) where TTarget : unmanaged, IRenderTarget
    {
        if (outputSize < RenderLimits.MinOutputSize) Throwers.InvalidArgument(nameof(outputSize));
        if (outputSize > RenderLimits.MaxOutputSize) Throwers.InvalidArgument(nameof(outputSize));

        if (_fboCount > RenderLimits.FboSlots) Throwers.InvalidOperation(nameof(_fboCount));
        if (_fboRegistry[_fboCount] != null!) Throwers.InvalidOperation(nameof(RenderRegistry));

        var gfxDescriptor = entry.Build(outputSize);
        var fboId = _gfxFbo.CreateFrameBuffer(gfxDescriptor);

        var sizePolicy = entry.FboSizePolicy ?? RenderFboSizePolicy.MakeDefault();

        var renderFbo = new RenderFbo(fboId, TargetRegistry<TTarget>.FboKey(variant), sizePolicy);
        if (typeof(TTarget) == typeof(ShadowPassTag))
        {
            if (entry.FboSizePolicy!.Mode != FboResizeMode.Fixed)
                Throwers.InvalidArgument("Shadow map require fixed size policy");

            renderFbo.IsShadowFbo = true;
            RenderContext.Instance.ShadowMapDimension = entry.FboSizePolicy!.FixedSize.Width;
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
        TargetRegistry<ShadowPassTag>.RegisterTag();
        TargetRegistry<ScenePassTag>.RegisterTag();
        TargetRegistry<LightPassTag>.RegisterTag();
        TargetRegistry<PostPassTag>.RegisterTag();
        TargetRegistry<OutputPassTag>.RegisterTag();
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
        private static byte _tagIndex;

        private static InlineArray4<byte> _passIds;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PassTagKey PassKey(FboVariant variant) =>
            new(_tagIndex, variant, new PassId(_passIds[variant]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FboTagKey FboKey(FboVariant variant) => new(_tagIndex, variant);

        public static PassTagKey BindFboPassId(FboVariant variant, PassId passId)
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

            _tagIndex = _targetCounter++;
            _isBound = true;
        }
    }
}