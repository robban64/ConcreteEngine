using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed unsafe class UniformUploader
{
    private readonly RenderUbo _drawUbo;
    private readonly RenderUbo _materialUbo;
    private readonly RenderUbo _animationUbo;

    private readonly DrawStateContext _ctx;
    private readonly GfxBuffers _gfxBuffers;
    private readonly MaterialBuffer _materialBuffer;
    private readonly EffectBuffer _effectBuffer;
    
    private static VisualRenderContext RenderContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => VisualRenderContext.Instance;
    }

    internal UniformUploader(DrawStateContext ctx, DrawStateContextPayload ctxPayload, RenderUploadBuffers buffers)
    {
        _ctx = ctx;
        _materialBuffer = buffers.Materials;
        _effectBuffer = buffers.Effects;

        _gfxBuffers = ctxPayload.Gfx.Buffers;
        var registry = ctxPayload.Registry.UboRegistry;

        _drawUbo = registry.GetRenderUbo<DrawObjectUniform>();
        _materialUbo = registry.GetRenderUbo<MaterialUniform>();
        _animationUbo = registry.GetRenderUbo<DrawAnimationUniform>();

        _animationUbo.SetCapacity(_animationUbo.Stride * 64);
        _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, _animationUbo.Capacity);
        
        UploadLight(); // set the buffer
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetCursor()
    {
        _drawUbo.ResetCursor();
        _materialUbo.ResetCursor();
        _animationUbo.ResetCursor();
    }

    public void EnsureDrawBuffers(uint drawCapacity, uint materialCapacity)
    {
        if (drawCapacity > _drawUbo.Capacity)
        {
            _drawUbo.SetCapacity(drawCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, drawCapacity);
        }

        if (materialCapacity > _materialUbo.Capacity)
        {
            _materialUbo.SetCapacity(materialCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_materialUbo.Id, drawCapacity);
        }
    }

    public ReadOnlySpan<TextureBinding> ResolveMaterial(MaterialId materialId, out RenderMaterialMeta materialMeta)
    {
        if (_ctx.ResolveMaterialBind(materialId))
        {
            BindMaterialObject(materialId);
            return _materialBuffer.GetMetaAndSlots(materialId, out materialMeta);
        }

        materialMeta = default;
        return ReadOnlySpan<TextureBinding>.Empty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMaterialObject(MaterialId matId)
    {
        var cursor = _materialUbo.SetDrawCursor(matId.Index());
        _gfxBuffers.BindUniformBufferRange(_materialUbo.Id, _materialUbo.Slot, cursor, _materialUbo.Stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindDrawObject(int submitIndex)
    {
        var cursor = _drawUbo.SetDrawCursor(submitIndex);
        _gfxBuffers.BindUniformBufferRange(_drawUbo.Id, _drawUbo.Slot, cursor, _drawUbo.Stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindAnimation(int slot)
    {
        var cursor = _animationUbo.SetDrawCursor(slot);
        _gfxBuffers.BindUniformBufferRange(_animationUbo.Id, cursor, _animationUbo.Stride);
    }

    public void UploadMaterial(NativeView<MaterialUniform> data) =>
        _gfxBuffers.UploadUniform(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));

    public void UploadDrawObjects(NativeView<DrawObjectUniform> data) =>
        _gfxBuffers.UploadUniform(_drawUbo.Id, data, _drawUbo.SetUploadCursor(0));


    public void UploadAnimationData(NativeView<Matrix4x4> boneData)
    {
        var uploadSize = _animationUbo.GetCapacityFor(boneData.Length);
        if (uploadSize > _animationUbo.Capacity)
        {
            _animationUbo.SetCapacity(uploadSize);
            _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, uploadSize);
        }

        _gfxBuffers.UploadUniform(_animationUbo.Id, boneData, 0);
        //_gfxBuffers.UploadUniformBytes(_animationUbo.Id, boneData.Reinterpret<byte>(), boneData.Length, stride, 0);
    }

    // Globals //
    public void UploadEditorEffectUniform(byte slot, bool isAnimated)
    {
        ref readonly var effect = ref _effectBuffer.GetResolveEffect(slot);
        var data = new EditorEffectsUniform(isAnimated, effect.Color);
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<EditorEffectsUniform>(), &data, 0);
    }


    [SkipLocalsInit]
    public void UploadCameraView()
    {
        var camera = RenderContext.Camera;

        var data = camera.UseLightSpace
            ? new CameraUniform(camera.Translation, in camera.LightMatrices)
            : new CameraUniform(camera.Translation, in camera.FrameMatrices);

        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<CameraUniform>(), &data, 0);
    }

    [SkipLocalsInit]
    public void UploadShadow()
    {
        ref readonly var shadow = ref RenderContext.Environment.GetShadow();
        var size = 1.0f / shadow.ShadowMapSize;

        ShadowUniform data;
        data.LightViewProj = RenderContext.Camera.LightMatrices.ProjectionViewMatrix;
        data.ShadowParams0 = new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias);
        data.ShadowParams1 = new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, shadow.Distance);

        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<ShadowUniform>(), &data, 0);
    }


    [SkipLocalsInit]
    public void UploadEngineUniformRecord(in RenderFrameArgs frameArgs)
    {
        var outputSize = RenderContext.OutputSize;
        CoordinateMath.ToUvCoords(frameArgs.MousePos, outputSize);

        var data = new EngineUniformRecord(
            invResolution: new Vector2(1.0f / outputSize.Width, 1.0f / outputSize.Height),
            mouse: CoordinateMath.ToUvCoords(frameArgs.MousePos, outputSize),
            deltaTime: RenderContext.DeltaTime,
            time: frameArgs.Time,
            random: frameArgs.Rng
        );

        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<EngineUniformRecord>(), &data, 0);
    }

    [SkipLocalsInit]
    public void UploadFrameUniformRecord()
    {
        ref readonly var fog = ref RenderContext.Environment.GetFog();
        ref readonly var ambient = ref RenderContext.Environment.GetAmbient();

        float kExp2 = 1f / (fog.Density * fog.Density);
        float kHeight = 1f / MathF.Max(x: fog.HeightFalloff, y: 1e-6f);

        FrameUniform data;
        data.Ambient = new Vector4(value: ambient.Ambient, w: ambient.Exposure);
        data.AmbientGround = new Vector4(value: ambient.AmbientGround, w: 0.0f);
        data.FogColor = new Vector4(value: fog.Color, w: fog.Scattering);
        data.FogParams0 = new Vector4(x: kExp2, y: kHeight, z: fog.BaseHeight, w: fog.Strength);
        data.FogParams1 = new Vector4(x: 1f, y: fog.HeightInfluence, z: fog.MaxDistance, w: 0.0f);

        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<FrameUniform>(), &data, 0);
    }

    [SkipLocalsInit]
    public void UploadDirLight()
    {
        ref readonly var dirLight = ref RenderContext.Environment.GetDirectionalLight();

        DirectionalLightUniform data;
        data.Direction = dirLight.Direction.AsVector4();
        data.Diffuse = new Vector4(dirLight.Diffuse, dirLight.Intensity);
        data.Specular = new Vector4(dirLight.Specular, 0.0f, 0.0f, 0.0f);

        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<DirectionalLightUniform>(), &data, 0);
    }

    [SkipLocalsInit]
    public void UploadPost()
    {
        ref readonly var post = ref RenderContext.Environment.GetPostEffect();

        PostFxUniform data;
        data.Grade = new Vector4(post.Grade.Exposure, post.Grade.Saturation, post.Grade.Contrast, post.Grade.Warmth);
        data.WhiteBalance = new Vector4(post.WhiteBalance.Tint, post.WhiteBalance.Strength, 0f, 0f);
        data.Bloom = new Vector4(post.Bloom.Intensity, post.Bloom.Threshold, post.Bloom.Radius, 0f);
        data.Fx = new Vector4(post.ImageFx.Vignette, post.ImageFx.Grain, post.ImageFx.Sharpen, post.ImageFx.Rolloff);
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<PostFxUniform>(), &data, 0);
    }


    public void UploadLight()
    {
        LightUniform data = default;
        _gfxBuffers.UploadSingleUniform(RenderUboRegistry.GetUboId<LightUniform>(), &data, 0);
    }
}