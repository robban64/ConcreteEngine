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
using ConcreteEngine.Renderer.State;

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


    private readonly GfxBuffers _gfxBuffers;

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

    private MaterialDrawBuffer _materialBuffer = null!;
    private readonly DrawStateContext _ctx;

    private readonly RenderParamsSnapshot _paramsSnapshot;

    private bool _hasUploadLight;


    internal DrawBuffers(DrawStateContext ctx, DrawStateContextPayload ctxPayload)
    {
        _ctx = ctx;

        _gfxBuffers = ctxPayload.Gfx.Buffers;
        _paramsSnapshot = ctxPayload.Snapshot;
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
    }

    public void AttachMaterialBuffer(MaterialDrawBuffer materialBuffer) => _materialBuffer = materialBuffer;

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

    public ReadOnlySpan<TextureSlotInfo> ResolveMaterial(MaterialId materialId, out RenderMaterialMeta materialMeta)
    {
        if (_ctx.ResolveMaterialBind(materialId))
        {
            BindMaterialObject(materialId);
            return _materialBuffer.GetMetaAndSlots(materialId, out materialMeta);
        }

        materialMeta = default;
        return ReadOnlySpan<TextureSlotInfo>.Empty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMaterialObject(MaterialId matId)
    {
        var cursor = _materialUbo.SetDrawCursor(matId.Id - 1);
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
    public void UploadGlobalUniforms(in RenderFrameArgs args)
    {
        UploadEngineUniformRecord(in args);
        if (!_hasUploadLight)
        {
            UploadLight();
            _hasUploadLight = true;
        }

        if (_paramsSnapshot.WasDirty)
        {
            UploadFrameUniformRecord();
            UploadDirLight();
            UploadPost();
        }
    }


    public void UploadCameraView(RenderCamera camera)
    {
        ref var data = ref DataStore.CameraData;
        if (!camera.UseLightViewOverride)
        {
            data.ViewMat = camera.RenderView.ViewMatrix;
            data.ProjMat = camera.RenderView.ProjectionMatrix;
            data.ProjViewMat = camera.RenderView.ProjectionViewMatrix;
            data.CameraPos = camera.Transform.Translation.AsVector4();
            data.CameraUp = camera.Up.AsVector4();
            data.CameraRight = camera.Right.AsVector4();
        }
        else
        {
            ref var view = ref camera.LightSpace;
            data.ViewMat = view.LightViewMatrix;
            data.ProjMat = view.LightProjectionMatrix;
            data.ProjViewMat = view.LightSpaceMatrix;
            data.CameraPos = camera.Transform.Translation.AsVector4();
            data.CameraUp = view.Up.AsVector4();
            data.CameraRight = view.Right.AsVector4();
        }

        _gfxBuffers.UploadUniformGpuData(_cameraUbo, in data, 0);
    }


    private void UploadEngineUniformRecord(in RenderFrameArgs args)
    {
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
        ref readonly var fog = ref _paramsSnapshot.Fog;
        ref readonly var ambient = ref _paramsSnapshot.Ambient;

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
        ref readonly var dirLight = ref _paramsSnapshot.SunLight;

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

    public void UploadShadow(in Matrix4x4 lightViewProjection)
    {
        ref readonly var shadow = ref _paramsSnapshot.Shadow;
        var size = 1.0f / shadow.ShadowMapSize;

        ref var data = ref DataStore.ShadowData;
        data.LightViewProj = lightViewProjection;
        data.ShadowParams0 = new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias);
        data.ShadowParams1 = new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, shadow.Distance);

        _gfxBuffers.UploadUniformGpuData(_shadowUbo, in data, 0);
    }

    private void UploadPost()
    {
        _paramsSnapshot.PostEffect
            .Deconstruct(out var g, out var wb, out var b, out var fx);

        ref var data = ref DataStore.PostData;
        data.Grade = new Vector4(g.Exposure, g.Saturation, g.Contrast, g.Warmth);
        data.WhiteBalance = new Vector4(wb.Tint, wb.Strength, 0f, 0f);
        data.Bloom = new Vector4(b.Intensity, b.Threshold, b.Radius, 0f);
        data.Fx = new Vector4(fx.Vignette, fx.Grain, fx.Sharpen, fx.Rolloff);
        _gfxBuffers.UploadUniformGpuData(_postUbo, in data, 0);
    }
}