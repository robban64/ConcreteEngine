using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Systems;

internal sealed unsafe class VisualSystem
{
    public static void Create(GfxBuffers gfx) => Instance = new VisualSystem(gfx);
    public static VisualSystem Instance { get; private set; } = null!;
    private static CameraManager CameraManager => CameraManager.Instance;
    private static VisualManager VisualManager => VisualManager.Instance;

    private readonly GfxBuffers _gfx;

    private VisualSystem(GfxBuffers gfx)
    {
        _gfx = gfx;
    }

    public void Upload()
    {
        UploadEngineUniformRecord();
        UploadMainView();

        if (!VisualManager.AnyWasDirty) return;

        if (VisualManager.Illumination.WasDirty)
            UploadDirLight();

        if (VisualManager.Illumination.WasDirty || VisualManager.Environment.WasDirty)
            UploadFrameUniformRecord();

        if (VisualManager.PostEffect.WasDirty)
            UploadPost();

        VisualManager.Commit();
    }

    public void UploadPointLight()
    {
        LightUniform data = default;
        _gfx.UploadSingleUniform(&data, 0);
    }


    [SkipLocalsInit]
    public void UploadMainView()
    {
        var t = CameraManager.FrameTransforms;
        CameraUniform data;
        data.ViewMat = t.ViewMatrix;
        data.ProjMat = t.ProjectionMatrix;
        data.ProjViewMat = t.ViewMatrix * t.ProjectionMatrix;
        data.CameraPos = t.Translation;
        data.CameraUp = t.Up;
        data.CameraRight = t.Right;
        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    public void UploadLightView()
    {
        var t = CameraManager.LightTransforms;
        CameraUniform data;
        data.ViewMat = t.ViewMatrix;
        data.ProjMat = t.ProjectionMatrix;
        data.ProjViewMat = t.ViewMatrix * t.ProjectionMatrix;
        data.CameraPos = t.Translation;
        data.CameraUp = t.Up;
        data.CameraRight = t.Right;
        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    public void UploadShadow()
    {
        var size = VisualManager.Shadow.InvMapSize;
        var proj = VisualManager.Shadow.Projection;
        var vis = VisualManager.Shadow.Visuals;

        ShadowUniform data;
        data.LightViewProj = CameraManager.LightTransforms.ViewMatrix * CameraManager.LightTransforms.ProjectionMatrix;
        data.ShadowParams0 = new Vector4(size, size, proj.ConstBias, proj.SlopeBias);
        data.ShadowParams1 = new Vector4(vis.Strength, vis.PcfRadius, 0.03f, proj.Distance);

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadEngineUniformRecord()
    {
        var mouse = CoordinateMath.ToUvCoords(EngineInput.Mouse.ViewportPos, EngineWindow.ViewportSize);
        var data = new EngineUniformRecord(
            invResolution: EngineWindow.InvViewport,
            mouse: mouse,
            deltaTime: EngineTime.DeltaTime,
            time: EngineTime.Time,
            random: EngineTime.FrameRng
        );

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadFrameUniformRecord()
    {
        var env = VisualManager.Environment;
        var fogHeight = env.FogHeight;
        var fogOptics = env.FogOptics;

        float kExp2 = 1f / (fogHeight.Density * fogHeight.Density);
        float kHeight = 1f / MathF.Max(fogHeight.HeightFalloff, 1e-6f);

        FrameUniform data;
        data.Ambient = new Vector4(env.Ambient, env.Exposure);
        data.AmbientGround = new Vector4(env.AmbientGround, 0.0f);

        data.FogColor = new Vector4(env.FogColor, fogOptics.Scattering);
        data.FogParams0 = new Vector4(kExp2, kHeight, fogHeight.BaseHeight, fogHeight.Strength);
        data.FogParams1 = new Vector4(fogOptics.DistanceWeight, fogOptics.HeightWeight, fogOptics.MaxDistance, 0.0f);

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadDirLight()
    {
        var it = VisualManager.Illumination;

        DirectionalLightUniform data;
        data.Direction = it.Direction.AsVector4();
        data.Diffuse = new Vector4(it.Diffuse, it.Intensity);
        data.Specular = new Vector4(it.Specular, 0.0f, 0.0f, 0.0f);

        _gfx.UploadSingleUniform(&data, 0);
    }

    [SkipLocalsInit]
    private void UploadPost()
    {
        var post = VisualManager.PostEffect;
        var bloom = post.Bloom;
        var wb = post.WhiteBalance;

        PostFxUniform data;
        data.Grade = Unsafe.BitCast<PostGradeParams, Vector4>(post.Grade);
        data.WhiteBalance = new Vector4(wb.Tint, wb.Strength, 0f, 0f);
        data.Bloom = new Vector4(bloom.Intensity, bloom.Threshold, bloom.Radius, 0f);
        data.Fx = Unsafe.BitCast<PostImageFxParams, Vector4>(post.ImageFx);

        _gfx.UploadSingleUniform(&data, 0);
    }
}