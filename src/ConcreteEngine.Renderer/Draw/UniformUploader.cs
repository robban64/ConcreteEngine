using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Draw;

internal sealed unsafe class UniformUploader
{
    private static class VisualStore
    {
        public static UniformBufferId LightUbo;
        public static UniformBufferId DirLightUbo;
        public static UniformBufferId FrameUbo;
        public static UniformBufferId PostUbo;
        public static UniformBufferId EditorEffectUbo;
    }

    private readonly RenderUbo _drawUbo;
    private readonly RenderUbo _materialUbo;
    private readonly RenderUbo _animationUbo;

    private readonly UniformBufferId _cameraUbo;
    private readonly UniformBufferId _shadowUbo;
    private readonly UniformBufferId _engineUbo;

    private readonly DrawStateContext _ctx;
    private readonly GfxBuffers _gfxBuffers;
    private MaterialBuffer _materialBuffer = null!;

    internal UniformUploader(DrawStateContext ctx, DrawStateContextPayload ctxPayload)
    {
        _ctx = ctx;

        _gfxBuffers = ctxPayload.Gfx.Buffers;
        var registry = ctxPayload.Registry.UboRegistry;

        _drawUbo = registry.GetRenderUbo<DrawUboTag>();
        _materialUbo = registry.GetRenderUbo<MaterialUboTag>();
        _animationUbo = registry.GetRenderUbo<DrawAnimationUboTag>();

        _engineUbo = registry.GetRenderUbo<EngineUboTag>().Id;
        _cameraUbo = registry.GetRenderUbo<CameraUboTag>().Id;
        _shadowUbo = registry.GetRenderUbo<ShadowUboTag>().Id;

        VisualStore.FrameUbo = registry.GetRenderUbo<FrameUboTag>().Id;
        VisualStore.DirLightUbo = registry.GetRenderUbo<DirLightUboTag>().Id;
        VisualStore.LightUbo = registry.GetRenderUbo<LightUboTag>().Id;
        VisualStore.PostUbo = registry.GetRenderUbo<PostUboTag>().Id;

        VisualStore.EditorEffectUbo = registry.GetRenderUbo<EditorEffectsUboTag>().Id;

        _animationUbo.SetCapacity(_animationUbo.Stride * 64);
        _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, _animationUbo.Capacity);
    }


    public void Initialize(MaterialBuffer materialBuffer)
    {
        _materialBuffer = materialBuffer;
        UploadLight();
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
    public void UploadGlobalUniforms()
    {
        UploadEngineUniformRecord();

        if (VisualRenderContext.Instance.Environment.WasDirty)
        {
            UploadFrameUniformRecord();
            UploadDirLight();
            UploadPost();
        }
    }

    public void UploadEditorEffectUniform(EditorEffectsUniform data) =>
        _gfxBuffers.UploadSingleUniform(VisualStore.EditorEffectUbo, &data, 0);


    [SkipLocalsInit]
    public void UploadCameraView()
    {
        var camera = VisualRenderContext.Instance.Camera;

        var data = camera.UseLightSpace
            ? new CameraUniform(camera.Translation, in camera.LightMatrices)
            : new CameraUniform(camera.Translation, in camera.FrameMatrices);

        _gfxBuffers.UploadSingleUniform(_cameraUbo, &data, 0);
    }

    [SkipLocalsInit]
    public void UploadShadow()
    {
        ref readonly var shadow = ref VisualRenderContext.Instance.Environment.GetShadow();
        var size = 1.0f / shadow.ShadowMapSize;

        ShadowUniform data;
        data.LightViewProj = VisualRenderContext.Instance.Camera.LightMatrices.ProjectionViewMatrix;
        data.ShadowParams0 = new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias);
        data.ShadowParams1 = new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, shadow.Distance);

        _gfxBuffers.UploadSingleUniform(_shadowUbo, &data, 0);
    }


    [SkipLocalsInit]
    private void UploadEngineUniformRecord()
    {
        ref readonly var args = ref VisualRenderContext.Instance.RenderFrameArgs;
        var data = new EngineUniformRecord(
            deltaTime: args.DeltaTime,
            invResolution: args.InvOutputSize,
            time: args.Time,
            mouse: args.MousePosUv,
            random: args.Rng
        );

        _gfxBuffers.UploadSingleUniform(_engineUbo, &data, 0);
    }

    [SkipLocalsInit]
    private void UploadFrameUniformRecord()
    {
        ref readonly var fog = ref VisualRenderContext.Instance.Environment.GetFog();
        ref readonly var ambient = ref VisualRenderContext.Instance.Environment.GetAmbient();

        float kExp2 = 1f / (fog.Density * fog.Density);
        float kHeight = 1f / MathF.Max(x: fog.HeightFalloff, y: 1e-6f);

        FrameUniform data;
        data.Ambient = new Vector4(value: ambient.Ambient, w: ambient.Exposure);
        data.AmbientGround = new Vector4(value: ambient.AmbientGround, w: 0.0f);
        data.FogColor = new Vector4(value: fog.Color, w: fog.Scattering);
        data.FogParams0 = new Vector4(x: kExp2, y: kHeight, z: fog.BaseHeight, w: fog.Strength);
        data.FogParams1 = new Vector4(x: 1f, y: fog.HeightInfluence, z: fog.MaxDistance, w: 0.0f);

        _gfxBuffers.UploadSingleUniform(VisualStore.FrameUbo, &data, 0);
    }

    [SkipLocalsInit]
    private void UploadDirLight()
    {
        ref readonly var dirLight = ref VisualRenderContext.Instance.Environment.GetDirectionalLight();

        DirectionalLightUniform data;
        data.Direction = dirLight.Direction.AsVector4();
        data.Diffuse = new Vector4(dirLight.Diffuse, dirLight.Intensity);
        data.Specular = new Vector4(dirLight.Specular, 0.0f, 0.0f, 0.0f);

        _gfxBuffers.UploadSingleUniform(VisualStore.DirLightUbo, &data, 0);
    }

    [SkipLocalsInit]
    private void UploadPost()
    {
        ref readonly var post = ref VisualRenderContext.Instance.Environment.GetPostEffect();

        PostProcessUniform data;
        data.Grade = new Vector4(post.Grade.Exposure, post.Grade.Saturation, post.Grade.Contrast, post.Grade.Warmth);
        data.WhiteBalance = new Vector4(post.WhiteBalance.Tint, post.WhiteBalance.Strength, 0f, 0f);
        data.Bloom = new Vector4(post.Bloom.Intensity, post.Bloom.Threshold, post.Bloom.Radius, 0f);
        data.Fx = new Vector4(post.ImageFx.Vignette, post.ImageFx.Grain, post.ImageFx.Sharpen, post.ImageFx.Rolloff);
        _gfxBuffers.UploadSingleUniform(VisualStore.PostUbo, &data, 0);
    }


    private void UploadLight()
    {
        LightUniform data = default;
        _gfxBuffers.UploadSingleUniform(VisualStore.LightUbo, &data, 0);
    }
}