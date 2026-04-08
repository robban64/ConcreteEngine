using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawBuffers
{
    private static class SpatialStore
    {
        public static UniformBufferId CameraUbo;
        public static UniformBufferId ShadowUbo;
        public static UniformBufferId EngineUbo;

        [FixedAddressValueType] public static EngineUniformRecord EngineUniformData;
        [FixedAddressValueType] public static CameraUniformRecord CameraData;
        [FixedAddressValueType] public static ShadowUniformRecord ShadowData;
    }

    private static class VisualStore
    {
        public static UniformBufferId LightUbo;
        public static UniformBufferId DirLightUbo;
        public static UniformBufferId FrameUbo;
        public static UniformBufferId PostUbo;
        public static UniformBufferId EditorEffectUbo;


        [FixedAddressValueType] public static DirLightUniformRecord DirLightData;
        [FixedAddressValueType] public static FrameUniformRecord FrameData;
        [FixedAddressValueType] public static PostProcessUniform PostData;
    }

    private bool _hasUploadLight;


    private readonly RenderUbo _drawUbo;
    private readonly RenderUbo _materialUbo;
    private readonly RenderUbo _animationUbo;

    private readonly DrawStateContext _ctx;
    private readonly GfxBuffers _gfxBuffers;
    private MaterialDrawBuffer _materialBuffer = null!;

    private readonly VisualRenderContext _visualContext = VisualRenderContext.Instance;

    internal DrawBuffers(DrawStateContext ctx, DrawStateContextPayload ctxPayload)
    {
        _ctx = ctx;

        _gfxBuffers = ctxPayload.Gfx.Buffers;
        var registry = ctxPayload.Registry.UboRegistry;

        _drawUbo = registry.GetRenderUbo<DrawUboTag>();
        _materialUbo = registry.GetRenderUbo<MaterialUboTag>();
        _animationUbo = registry.GetRenderUbo<DrawAnimationUboTag>();

        SpatialStore.EngineUbo = registry.GetRenderUbo<EngineUboTag>().Id;
        SpatialStore.CameraUbo = registry.GetRenderUbo<CameraUboTag>().Id;
        SpatialStore.ShadowUbo = registry.GetRenderUbo<ShadowUboTag>().Id;

        VisualStore.FrameUbo = registry.GetRenderUbo<FrameUboTag>().Id;
        VisualStore.DirLightUbo = registry.GetRenderUbo<DirLightUboTag>().Id;
        VisualStore.LightUbo = registry.GetRenderUbo<LightUboTag>().Id;
        VisualStore.PostUbo = registry.GetRenderUbo<PostUboTag>().Id;
        
        VisualStore.EditorEffectUbo = registry.GetRenderUbo<EditorEffectsUboTag>().Id;

        _animationUbo.SetCapacity(_animationUbo.Stride * 64);
        _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, _animationUbo.Capacity);

    }


    public void Initialize(MaterialDrawBuffer materialBuffer)
    {
        _materialBuffer = materialBuffer;
        if (!_hasUploadLight)
        {
            UploadLight();
            _hasUploadLight = true;
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetCursor()
    {
        _drawUbo.ResetCursor();
        _materialUbo.ResetCursor();
        _animationUbo.ResetCursor();
    }

    public void EnsureDrawBuffers(nint drawCapacity, nint materialCapacity)
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
        _gfxBuffers.BindUniformBufferRange(_materialUbo.Id, cursor, _materialUbo.Stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindDrawObject(int submitIndex)
    {
        var cursor = _drawUbo.SetDrawCursor(submitIndex);
        _gfxBuffers.BindUniformBufferRange(_drawUbo.Id, cursor, _drawUbo.Stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindAnimation(int slot)
    {
        var cursor = _animationUbo.SetDrawCursor(slot);
        _gfxBuffers.BindUniformBufferRange(_animationUbo.Id, cursor, _animationUbo.Stride);
    }

    public void UploadMaterialRecord(in MaterialUniformRecord data) =>
        _gfxBuffers.UploadUniformGpuItem(_materialUbo.Id, in data, 0);

    public void UploadMaterial(NativeView<MaterialUniformRecord> data) =>
        _gfxBuffers.UploadUniformGpuSpan(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));

    public void UploadDrawObjects(NativeView<DrawObjectUniform> data) =>
        _gfxBuffers.UploadUniformGpuSpan(_drawUbo.Id, data, _drawUbo.SetUploadCursor(0));


    public void UploadAnimationData(NativeView<Matrix4x4> boneData)
    {
        var uploadSize = _animationUbo.GetCapacityFor(boneData.Length);
        if (uploadSize > _animationUbo.Capacity)
        {
            _animationUbo.SetCapacity(uploadSize);
            _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, uploadSize);
        }

        _gfxBuffers.UploadUniformBytes(_animationUbo.Id, boneData.Reinterpret<byte>(), Unsafe.SizeOf<Matrix4x4>(),
            boneData.Length, 0);

        //_gfxBuffers.UploadUniformGpuSpan(_animationUbo.Id, boneData, 0);
    }

    // Globals //
    public void UploadGlobalUniforms()
    {
        UploadEngineUniformRecord();

        if (_visualContext.Environment.WasDirty)
        {
            UploadFrameUniformRecord();
            UploadDirLight();
            UploadPost();
        }
    }
    public void UploadEditorEffectUniform(in EditorEffectsUniform data) =>
        _gfxBuffers.UploadUniformGpuItem(VisualStore.EditorEffectUbo, in data, 0);


    public void UploadCameraView()
    {
        var camera = _visualContext.Camera;
        ref var data = ref SpatialStore.CameraData;

        data.CameraPos = camera.Translation;
        if (camera.UseLightSpace)
            data.FillView(in camera.LightMatrices);
        else
            data.FillView(in camera.FrameMatrices);

        _gfxBuffers.UploadUniformGpuItem(SpatialStore.CameraUbo, in data, 0);
    }
    
    public void UploadShadow()
    {
        ref readonly var shadow = ref _visualContext.Environment.GetShadow();
        var size = 1.0f / shadow.ShadowMapSize;

        ref var data = ref SpatialStore.ShadowData;
        data.LightViewProj = SpatialStore.CameraData.ProjViewMat;
        data.ShadowParams0 = new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias);
        data.ShadowParams1 = new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, shadow.Distance);

        _gfxBuffers.UploadUniformGpuItem(SpatialStore.ShadowUbo, in data, 0);
    }


    private  void UploadEngineUniformRecord()
    {
        ref readonly var args = ref _visualContext.RenderFrameArgs;
        ref var data = ref SpatialStore.EngineUniformData;
        data = new EngineUniformRecord(
            deltaTime: args.DeltaTime,
            invResolution: args.InvOutputSize,
            time: args.Time,
            mouse: args.MousePosUv,
            random: args.Rng
        );

        _gfxBuffers.UploadUniformGpuItem(SpatialStore.EngineUbo, in data, 0);
    }

    private void UploadFrameUniformRecord()
    {
        ref readonly var fog = ref _visualContext.Environment.GetFog();
        ref readonly var ambient = ref _visualContext.Environment.GetAmbient();

        float kExp2 = 1f / (fog.Density * fog.Density);
        float kHeight = 1f / MathF.Max(x: fog.HeightFalloff, y: 1e-6f);

        ref var data = ref VisualStore.FrameData;
        data.Ambient = new Vector4(value: ambient.Ambient, w: ambient.Exposure);
        data.AmbientGround = new Vector4(value: ambient.AmbientGround, w: 0.0f);
        data.FogColor = new Vector4(value: fog.Color, w: fog.Scattering);
        data.FogParams0 = new Vector4(x: kExp2, y: kHeight, z: fog.BaseHeight, w: fog.Strength);
        data.FogParams1 = new Vector4(x: 1f, y: fog.HeightInfluence, z: fog.MaxDistance, w: 0.0f);

        _gfxBuffers.UploadUniformGpuItem(VisualStore.FrameUbo, in data, 0);
    }

    private void UploadDirLight()
    {
        ref readonly var dirLight = ref _visualContext.Environment.GetDirectionalLight();

        ref var data = ref VisualStore.DirLightData;
        data.Direction = dirLight.Direction.AsVector4();
        data.Diffuse = new Vector4(dirLight.Diffuse, dirLight.Intensity);
        data.Specular = new Vector4(dirLight.Specular, 0.0f, 0.0f, 0.0f);

        _gfxBuffers.UploadUniformGpuItem(VisualStore.DirLightUbo, in data, 0);
    }

    private void UploadLight()
    {
        _gfxBuffers.UploadUniformGpuItem<LightUniformRecord>(VisualStore.LightUbo, default, 0);
    }

    

    private void UploadPost()
    {
        ref readonly var post = ref _visualContext.Environment.GetPostEffect();

        ref var data = ref VisualStore.PostData;
        data.Grade = new Vector4(post.Grade.Exposure, post.Grade.Saturation, post.Grade.Contrast, post.Grade.Warmth);
        data.WhiteBalance = new Vector4(post.WhiteBalance.Tint, post.WhiteBalance.Strength, 0f, 0f);
        data.Bloom = new Vector4(post.Bloom.Intensity, post.Bloom.Threshold, post.Bloom.Radius, 0f);
        data.Fx = new Vector4(post.ImageFx.Vignette, post.ImageFx.Grain, post.ImageFx.Sharpen, post.ImageFx.Rolloff);
        _gfxBuffers.UploadUniformGpuItem(VisualStore.PostUbo, in data, 0);
    }
}