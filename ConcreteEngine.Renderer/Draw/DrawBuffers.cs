#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawBuffers
{
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

    private MaterialDrawBuffer _materialBuffer = null!;
    private readonly DrawStateContext _ctx;

    private readonly RenderSceneSnapshot _sceneSnapshot;

    internal DrawBuffers(DrawStateContext ctx, DrawStateContextPayload ctxPayload)
    {
        _ctx = ctx;

        _gfxBuffers = ctxPayload.Gfx.Buffers;
        _sceneSnapshot = ctxPayload.Snapshot;
        var registry = ctxPayload.Registry;

        _drawUbo = registry.GetRenderUbo<DrawObjectUniform>();
        _materialUbo = registry.GetRenderUbo<MaterialUniformRecord>();

        _engineUbo = registry.GetRenderUbo<EngineUniformRecord>().Id;
        _frameUbo = registry.GetRenderUbo<FrameUniformRecord>().Id;
        _cameraUbo = registry.GetRenderUbo<CameraUniformRecord>().Id;
        _dirLightUbo = registry.GetRenderUbo<DirLightUniformRecord>().Id;
        _lightUbo = registry.GetRenderUbo<LightUniformRecord>().Id;
        _shadowUbo = registry.GetRenderUbo<ShadowUniformRecord>().Id;
        _postUbo = registry.GetRenderUbo<PostProcessUniform>().Id;
    }

    public void AttachMaterialBuffer(MaterialDrawBuffer materialBuffer) => _materialBuffer = materialBuffer;

    public void ResetCursor()
    {
        _drawUbo.ResetCursor();
        _materialUbo.ResetCursor();
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

    public ReadOnlySpan<TextureSlotInfo> ResolveMaterial(MaterialId materialId, out DrawMaterialMeta materialMeta)
    {
        if (_ctx.ResolveMaterialBind(materialId))
            return _materialBuffer.GetMetaAndSlots(materialId, out materialMeta);

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

    public void UploadMaterialRecord(MaterialId materialId, in MaterialUniformRecord data)
        => _gfxBuffers.UploadUniformGpuData(_materialUbo.Id, in data, 0);

    public void UploadMaterial(ReadOnlySpan<MaterialUniformRecord> data)
        => _gfxBuffers.UploadUniformGpuSpan(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));

    public void UploadDrawObjects(ReadOnlySpan<DrawObjectUniform> data) =>
        _gfxBuffers.UploadUniformGpuSpan(_drawUbo.Id, data, _drawUbo.SetUploadCursor(0));


    // Globals //
    public void UploadGlobalUniforms(in RenderFrameInfo frameInfo, in RenderRuntimeParams runtimeParams)
    {
        UploadEngineUniformRecord(in frameInfo, in runtimeParams);
        UploadLight();
        if (_sceneSnapshot.IsDirty)
        {
            UploadFrameUniformRecord();
            UploadDirLight();
            UploadPost();
        }
    }

    public void UploadCameraView(RenderView view)
    {
        view.GetCurrentData(out var viewMat, out var projMat, out var projViewMat);
        var data = new CameraUniformRecord(
            viewMat: in viewMat,
            projMat: in projMat,
            projViewMat: in projViewMat,
            cameraPos: view.Position
        );

        _gfxBuffers.UploadUniformGpuData(_cameraUbo, in data, 0);
    }

    private void UploadEngineUniformRecord(in RenderFrameInfo frameInfo, in RenderRuntimeParams runtimeParams)
    {
        var outputSize = frameInfo.OutputSize;
        var data = new EngineUniformRecord(
            deltaTime: frameInfo.DeltaTime,
            invResolution: new Vector2(1.0f / outputSize.Width, 1.0f / outputSize.Height),
            time: runtimeParams.Time,
            mouse: runtimeParams.MousePos,
            random: runtimeParams.RndSeed
        );

        _gfxBuffers.UploadUniformGpuData(_engineUbo, in data, 0);
    }

    private void UploadFrameUniformRecord()
    {
        var fog = _sceneSnapshot.Fog;
        var ambient = _sceneSnapshot.Ambient;

        float kExp2 = 1f / (fog.Density * fog.Density);
        float kHeight = 1f / MathF.Max(x: fog.HeightFalloff, y: 1e-6f);

        var data = new FrameUniformRecord(
            ambient: new Vector4(value: ambient.Ambient, w: ambient.Exposure),
            ambientGround: new Vector4(value: ambient.AmbientGround, w: 0.0f),
            fogColor: new Vector4(value: fog.Color, w: fog.Scattering),
            fogParams0: new Vector4(x: kExp2, y: kHeight, z: fog.BaseHeight, w: fog.Strength),
            fogParams1: new Vector4(x: 1f, y: fog.HeightInfluence, z: fog.MaxDistance, w: 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(uboId: _frameUbo, data: in data, offset: 0);
    }

    private void UploadDirLight()
    {
        var dirLight = _sceneSnapshot.DirLight;
        var data = new DirLightUniformRecord(
            direction: dirLight.Direction.AsVector4(),
            diffuse: new Vector4(dirLight.Diffuse, dirLight.Intensity),
            specular: new Vector4(dirLight.Specular, 0.0f, 0.0f, 0.0f)
        );
        _gfxBuffers.UploadUniformGpuData(_dirLightUbo, in data, 0);
    }

    private void UploadLight()
    {
        var data = new LightUniformRecord(0, default);
        _gfxBuffers.UploadUniformGpuData(_lightUbo, in data, 0);
    }

    public void UploadShadow(in Matrix4x4 lightViewProjection)
    {
        //0.001f, 0.005f
        // 0.0004f, 0.0025f
        
        var shadow = _sceneSnapshot.Shadows;
        var size = 1.0f / shadow.ShadowMapSize;
        var data = new ShadowUniformRecord(
            lightViewProj: lightViewProjection,
            shadowParams0: new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias),
            shadowParams1: new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_shadowUbo, in data, 0);
    }

    private void UploadPost()
    {
        /*
        var data = new PostProcessUniform(
            grade: new Vector4(-0.015f, 1.10f, 0.96f, 0.018f),
            whiteBalance: new Vector4(-0.003f, 0.25f, 0.0f, 0.0f),
            bloom: new Vector4(0.55f, 0.78f, 1.10f, 0.0f),
            fx: new Vector4(0.04f, 0.0025f, 0.065f, 0.095f)
        );
*/
        var effect = _sceneSnapshot.PostEffects;
        var (g, wb, b, fx) = (effect.Grade, effect.WhiteBalance, effect.Bloom, Fx: effect.ImageFx);
        var data = new PostProcessUniform(
            grade: new Vector4(g.Exposure * 0.10f, 0.8f + g.Saturation * 0.4f, 0.9f + g.Contrast * 0.2f,
                g.Warmth * 0.05f),
            whiteBalance: new Vector4(wb.Tint * 0.05f, wb.Strength, 0f, 0f),
            bloom: new Vector4(b.Intensity * 1.5f, 0.6f + b.Threshold * 0.3f, b.Radius, 0f),
            fx: new Vector4(fx.Vignette * 0.15f, fx.Grain * 0.01f, fx.Sharpen * 0.15f, fx.Rolloff * 0.12f)
        );
        _gfxBuffers.UploadUniformGpuData(_postUbo, in data, 0);
    }
}