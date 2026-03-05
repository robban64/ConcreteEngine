using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    private static class DataStore
    {
        public static CameraUniformRecord CameraData;

        //public static LightUniformRecord LightData;
        public static DirLightUniformRecord DirLightData;
        public static FrameUniformRecord FrameData;
        public static ShadowUniformRecord ShadowData;
        public static PostProcessUniform PostData;
    }


    private readonly UniformBufferId _engineUbo;
    private readonly UniformBufferId _frameUbo;
    private readonly UniformBufferId _cameraUbo;
    private readonly UniformBufferId _lightUbo;
    private readonly UniformBufferId _shadowUbo;
    private readonly UniformBufferId _dirLightUbo;
    private readonly UniformBufferId _postUbo;


    private readonly RenderUbo _drawUbo;
    private readonly RenderUbo _materialUbo;
    private readonly RenderUbo _animationUbo;

    private readonly GfxBuffers _gfxBuffers;

    private MaterialDrawBuffer _materialBuffer = null!;
    private readonly DrawStateContext _ctx;

    private bool _hasUploadLight;

    private VisualEnvironment VisualEnv => VisualRenderContext.Instance.Visuals;

    internal DrawBuffers(DrawStateContext ctx, DrawStateContextPayload ctxPayload)
    {
        _ctx = ctx;

        _gfxBuffers = ctxPayload.Gfx.Buffers;
        var registry = ctxPayload.Registry.UboRegistry;

        _drawUbo = registry.GetRenderUbo<DrawUboTag>();
        _materialUbo = registry.GetRenderUbo<MaterialUboTag>();

        _animationUbo = registry.GetRenderUbo<DrawAnimationUboTag>();

        _engineUbo = registry.GetRenderUbo<EngineUboTag>().Id;
        _frameUbo = registry.GetRenderUbo<FrameUboTag>().Id;
        _cameraUbo = registry.GetRenderUbo<CameraUboTag>().Id;
        _dirLightUbo = registry.GetRenderUbo<DirLightUboTag>().Id;
        _lightUbo = registry.GetRenderUbo<LightUboTag>().Id;
        _shadowUbo = registry.GetRenderUbo<ShadowUboTag>().Id;
        _postUbo = registry.GetRenderUbo<PostUboTag>().Id;

        _animationUbo.SetCapacity(_animationUbo.Stride * 64);
        _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, _animationUbo.Capacity);

        RuntimeHelpers.RunClassConstructor(typeof(DataStore).TypeHandle);
    }


    public void Initialize(MaterialDrawBuffer materialBuffer) => _materialBuffer = materialBuffer;

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

    public void UploadMaterialRecord(MaterialId materialId, in MaterialUniformRecord data) =>
        _gfxBuffers.UploadUniformGpuData(_materialUbo.Id, in data, 0);

    public void UploadMaterial(ReadOnlySpan<MaterialUniformRecord> data) =>
        _gfxBuffers.UploadUniformGpuSpan(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));

    public void UploadDrawObjects(ReadOnlySpan<DrawObjectUniform> data) =>
        _gfxBuffers.UploadUniformGpuSpan(_drawUbo.Id, data, _drawUbo.SetUploadCursor(0));

    public void UploadAnimationData(ReadOnlySpan<Matrix4x4> boneData)
    {
        var uploadSize = _animationUbo.GetCapacityFor(boneData.Length);
        if (uploadSize > _animationUbo.Capacity)
        {
            _animationUbo.SetCapacity(uploadSize);
            _gfxBuffers.SetUniformBufferCapacity(_animationUbo.Id, uploadSize);
        }

        _gfxBuffers.UploadUniformBytes(_animationUbo.Id, MemoryMarshal.AsBytes(boneData), Unsafe.SizeOf<Matrix4x4>(),
            boneData.Length, 0);

        //_gfxBuffers.UploadUniformGpuSpan(_animationUbo.Id, boneData, 0);
    }

    // Globals //
    public void UploadGlobalUniforms()
    {
        UploadEngineUniformRecord();
        if (!_hasUploadLight)
        {
            UploadLight();
            _hasUploadLight = true;
        }

        if (VisualRenderContext.Instance.Visuals.WasDirty)
        {
            UploadFrameUniformRecord();
            UploadDirLight();
            UploadPost();
        }
    }


    public void UploadCameraView()
    {
        ref var data = ref DataStore.CameraData;

        data.CameraPos = VisualRenderContext.Instance.Camera.Translation;

        if (VisualRenderContext.Instance.UseLightSpace)
            data.FillView(in VisualRenderContext.Instance.Camera.GetLightMatrices());
        else
            data.FillView(in VisualRenderContext.Instance.Camera.GetFrameMatrices());

        _gfxBuffers.UploadUniformGpuData(_cameraUbo, in data, 0);
    }


    private void UploadEngineUniformRecord()
    {
        ref readonly var args = ref VisualRenderContext.Instance.RenderFrameArgs;
        var outputSize = args.OutputSize;
        var invRes = new Vector2(1.0f / outputSize.Width, 1.0f / outputSize.Height);

        var data = new EngineUniformRecord(
            deltaTime: args.DeltaTime,
            invResolution: invRes,
            time: args.Time,
            mouse: CoordinateMath.ToUvCoords(args.MousePos, outputSize),
            random: args.Rng
        );

        _gfxBuffers.UploadUniformGpuData(_engineUbo, in data, 0);
    }

    private void UploadFrameUniformRecord()
    {
        
        ref readonly var fog = ref VisualEnv.GetFog();
        ref readonly var ambient = ref VisualEnv.GetAmbient();

        float kExp2 = 1f / (fog.Density * fog.Density);
        float kHeight = 1f / MathF.Max(x: fog.HeightFalloff, y: 1e-6f);

        ref var data = ref DataStore.FrameData;
        data.Ambient = new Vector4(value: ambient.Ambient, w: ambient.Exposure);
        data.AmbientGround = new Vector4(value: ambient.AmbientGround, w: 0.0f);
        data.FogColor = new Vector4(value: fog.Color, w: fog.Scattering);
        data.FogParams0 = new Vector4(x: kExp2, y: kHeight, z: fog.BaseHeight, w: fog.Strength);
        data.FogParams1 = new Vector4(x: 1f, y: fog.HeightInfluence, z: fog.MaxDistance, w: 0.0f);

        _gfxBuffers.UploadUniformGpuData(_frameUbo, in data, 0);
    }

    private void UploadDirLight()
    {
        ref readonly var dirLight = ref VisualEnv.GetDirectionalLight();

        ref var data = ref DataStore.DirLightData;
        data.Direction = dirLight.Direction.AsVector4();
        data.Diffuse = new Vector4(dirLight.Diffuse, dirLight.Intensity);
        data.Specular = new Vector4(dirLight.Specular, 0.0f, 0.0f, 0.0f);

        _gfxBuffers.UploadUniformGpuData(_dirLightUbo, in data, 0);
    }

    private void UploadLight()
    {
        _gfxBuffers.UploadUniformGpuData<LightUniformRecord>(_lightUbo, default, 0);
    }

    public void UploadShadow()
    {
        ref readonly var shadow = ref VisualEnv.GetShadow();
        var size = 1.0f / shadow.ShadowMapSize;

        ref var data = ref DataStore.ShadowData;
        data.LightViewProj = VisualRenderContext.Instance.Camera.GetLightMatrices().ProjectionViewMatrix;
        data.ShadowParams0 = new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias);
        data.ShadowParams1 = new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, shadow.Distance);

        _gfxBuffers.UploadUniformGpuData(_shadowUbo, in data, 0);
    }

    private void UploadPost()
    {
        ref readonly var post = ref VisualEnv.GetPostEffect();

        ref var data = ref DataStore.PostData;
        data.Grade = new Vector4(post.Grade.Exposure, post.Grade.Saturation, post.Grade.Contrast, post.Grade.Warmth);
        data.WhiteBalance = new Vector4(post.WhiteBalance.Tint, post.WhiteBalance.Strength, 0f, 0f);
        data.Bloom = new Vector4(post.Bloom.Intensity, post.Bloom.Threshold, post.Bloom.Radius, 0f);
        data.Fx = new Vector4(post.ImageFx.Vignette, post.ImageFx.Grain, post.ImageFx.Sharpen, post.ImageFx.Rolloff);
        _gfxBuffers.UploadUniformGpuData(_postUbo, in data, 0);
    }
}