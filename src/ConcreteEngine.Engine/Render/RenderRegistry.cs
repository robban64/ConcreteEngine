using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

public sealed partial class RenderRegistry
{
    internal static RenderRegistry Instance { get; private set; } = null!;

    internal static void Create(GfxContext gfx)
    {
        if (Instance != null!) Throwers.InvalidOperation("Already created");
        Instance = new RenderRegistry(gfx);
    }


    private int _fboCount;
    private int _passCount;

    private readonly RenderFbo[] _frameBuffers;
    private readonly InlineArray4<byte>[] _slotByTagIndex;

    private readonly RenderPassEntry[] _passEntries;

    private readonly GfxFrameBuffers _gfxFbo;


    private RenderRegistry(GfxContext gfx)
    {
        _gfxFbo = gfx.FrameBuffers;
        _passEntries = new RenderPassEntry[RenderLimits.FboSlots];
        _frameBuffers = new RenderFbo[RenderLimits.FboSlots];
        _slotByTagIndex = new InlineArray4<byte>[RenderLimits.FboSlots];
        for (int i = 0; i < RenderLimits.FboSlots; i++)
        {
            for (int j = 0; j < 4; j++) _slotByTagIndex[i][j] = byte.MaxValue;
        }

        CreateUniformBuffers(gfx.Buffers);
        RegisterFbo();
    }


    private ReadOnlySpan<RenderFbo> GetFrameBuffers() => new(_frameBuffers, 0, _fboCount);
    private ReadOnlySpan<RenderPassEntry> GetPassEntries() => new(_passEntries, 0, _passCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RenderFbo GetByKey(FboKey key)
    {
        var index = _slotByTagIndex[key.TagIndex][key.Variant];
        return _frameBuffers[index];
    }

    public void RecreateFixedFrameBuffer<TTarget>(FboVariant v, Size2D size) where TTarget : unmanaged, IRenderTarget
    {
        RecreateFixedFrameBuffer(TargetRegistry<TTarget>.TagIndex, v, size);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecreateFixedFrameBuffer(byte tagIndex, FboVariant variant, Size2D size)
    {
        if (variant < 0 || variant > RenderLimits.MaxFboVariants) Throwers.InvalidArgument(nameof(variant));

        var fbo = GetByKey(new FboKey(tagIndex, variant));
        if (fbo == null!) Throwers.NotFoundBy(nameof(variant), variant.Value);

        var meta = GfxRegistry.GetMeta(fbo.FboId);
        if (meta.Size == size) return;

        RenderFbo.ValidateOutputSize(size, fbo.IsShadowFbo);
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
        RenderFbo.ValidateOutputSize(outputSize, false);

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

    internal RenderPassEntry RegisterPass<TTarget>(FboVariant variant, PassOp op, GfxPassState gfxState,
        ShaderId shaderId = default, bool linearFilter = false)
        where TTarget : unmanaged, IRenderTarget
    {
        var key = TargetRegistry<TTarget>.BindPassTarget(variant, new PassId(_passCount));
        return AddPassEntry(key, op, gfxState, shaderId, linearFilter);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RenderPassEntry AddPassEntry(PassTargetKey key, PassOp op, GfxPassState gfxState, ShaderId shaderId,
        bool linearFilter)
    {
        foreach (var e in GetPassEntries())
        {
            if (e.PassKey == key) Throwers.InvalidArgument("Duplicated passes");
        }

        FrameBufferId fboId = default;
        foreach (var e in GetFrameBuffers())
        {
            if (e.Key == key) fboId = e.FboId;
        }

        return _passEntries[_passCount++] = new RenderPassEntry(key, op, gfxState, fboId, shaderId, linearFilter);
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

        RenderFbo.ValidateOutputSize(entry.Size, targetKind == RenderTargetKind.Shadow);

        var fboId = _gfxFbo.CreateFrameBuffer(entry);
        var renderFbo = new RenderFbo(fboId, key, targetKind, resizeMode, calc);
        if (targetKind == RenderTargetKind.Shadow)
            RenderContext.DepthTexture = GfxRegistry.GetMeta(fboId).Attachments.DepthTexture;

        _slotByTagIndex[key.TagIndex][key.Variant] = (byte)_fboCount;
        _frameBuffers[_fboCount++] = renderFbo;
    }

    private static void RegisterFbo()
    {
        TargetRegistry<ShadowTarget>.RegisterTag();
        TargetRegistry<SceneTarget>.RegisterTag();
        TargetRegistry<LightTarget>.RegisterTag();
        TargetRegistry<PostFxTarget>.RegisterTag();
        TargetRegistry<OutputTarget>.RegisterTag();
    }

    private static void CreateUniformBuffers(GfxBuffers gfxBuffers)
    {
        EngineUniformRecord.UboId = gfxBuffers.CreateUniformBuffer<EngineUniformRecord>();
        EnvironmentUniform.UboId = gfxBuffers.CreateUniformBuffer<EnvironmentUniform>();
        CameraUniform.UboId = gfxBuffers.CreateUniformBuffer<CameraUniform>();
        LightningUniform.UboId = gfxBuffers.CreateUniformBuffer<LightningUniform>();
        PointLightUniform.UboId = gfxBuffers.CreateUniformBuffer<PointLightUniform>();
        ShadowUniform.UboId = gfxBuffers.CreateUniformBuffer<ShadowUniform>();
        MaterialUniform.UboId = gfxBuffers.CreateUniformBuffer<MaterialUniform>();
        TransformUniform.UboId = gfxBuffers.CreateUniformBuffer<TransformUniform>();
        SkinningUniform.UboId = gfxBuffers.CreateUniformBuffer<SkinningUniform>();
        PostFxUniform.UboId = gfxBuffers.CreateUniformBuffer<PostFxUniform>();
        EditorEffectsUniform.UboId = gfxBuffers.CreateUniformBuffer<EditorEffectsUniform>();
    }
}