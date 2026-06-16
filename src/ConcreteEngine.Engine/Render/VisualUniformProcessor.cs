using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;

public static unsafe class VisualUniformProcessor
{
    private static CameraManager CameraManager => CameraManager.Instance;
    private static VisualManager VisualManager => VisualManager.Instance;

    public static UniformUploaderCallbacks MakeCallbacks()
    {
        return new UniformUploaderCallbacks
        {
            UploadMainView = &UploadMainView, UploadLightView = &UploadLightView, UploadShadow = &UploadShadow
        };
    }

    public static void Upload(UniformUploadContext ctx)
    {
        var visuals = VisualManager;
        UploadEngineUniformRecord(ctx, EngineWindow.Viewport.Size, EngineInput.Mouse.ViewportPos);

        if (!visuals.AnyWasDirty) return;

        if (visuals.Illumination.WasDirty)
            UploadDirLight(in ctx);

        if (visuals.Illumination.WasDirty || visuals.Environment.WasDirty)
            UploadFrameUniformRecord(in ctx);

        if (visuals.PostEffect.WasDirty)
            UploadPost(in ctx);
        
        visuals.Commit();
    }


    [SkipLocalsInit]
    public static void UploadMainView(in UniformUploadContext ctx)
    {
        var t = CameraManager.FrameTransforms;
        CameraUniform data;
        data.ViewMat = t.ViewMatrix;
        data.ProjMat = t.ProjectionMatrix;
        data.ProjViewMat = t.ViewMatrix * t.ProjectionMatrix;
        data.CameraPos = t.Translation;
        data.CameraUp = t.Up;
        data.CameraRight = t.Right;
        ctx.UploadUniform(&data);
    }

    [SkipLocalsInit]
    public static void UploadLightView(in UniformUploadContext ctx)
    {
        var t = CameraManager.LightTransforms;
        CameraUniform data;
        data.ViewMat = t.ViewMatrix;
        data.ProjMat = t.ProjectionMatrix;
        data.ProjViewMat = t.ViewMatrix * t.ProjectionMatrix;
        data.CameraPos = t.Translation;
        data.CameraUp = t.Up;
        data.CameraRight = t.Right;
        ctx.UploadUniform(&data);
    }

    [SkipLocalsInit]
    public static void UploadShadow(in UniformUploadContext ctx)
    {
        var shadow = VisualManager.Shadow;
        var t = CameraManager.LightTransforms;

        ref readonly var proj = ref shadow.Projection.Value;
        ref readonly var vis = ref shadow.Visuals.Value;

        var size = 1.0f / shadow.ShadowMapSize;

        ShadowUniform data;
        data.LightViewProj = t.ViewMatrix * t.ProjectionMatrix;
        data.ShadowParams0 = new Vector4(size, size, proj.ConstBias, proj.SlopeBias);
        data.ShadowParams1 = new Vector4(vis.Strength, vis.PcfRadius, 0.03f, proj.Distance);

        ctx.UploadUniform(&data);
    }

    [SkipLocalsInit]
    private static void UploadEngineUniformRecord(in UniformUploadContext ctx, Size2D outputSize, Vector2 mouse)
    {
        var data = new EngineUniformRecord(
            invResolution: new Vector2(1.0f / outputSize.Width, 1.0f / outputSize.Height),
            mouse: CoordinateMath.ToUvCoords(mouse, outputSize),
            deltaTime: EngineTime.DeltaTime,
            time: EngineTime.Time,
            random: EngineTime.FrameRng
        );

        ctx.UploadUniform(&data);
    }

    [SkipLocalsInit]
    private static void UploadFrameUniformRecord(in UniformUploadContext ctx)
    {
        var visualManager = VisualManager;

        ref readonly var fogHeight = ref visualManager.Environment.FogHeight.Value;
        ref readonly var fogOptics = ref visualManager.Environment.FogOptics.Value;
        ref readonly var ambient = ref visualManager.Illumination.Ambient.Value;

        float kExp2 = 1f / (fogHeight.Density * fogHeight.Density);
        float kHeight = 1f / MathF.Max(x: fogHeight.HeightFalloff, y: 1e-6f);

        FrameUniform data;
        data.Ambient = new Vector4(value: ambient.Ambient, w: ambient.Exposure);
        data.AmbientGround = new Vector4(value: ambient.AmbientGround, w: 0.0f);

        data.FogColor = new Vector4(value: fogOptics.Color, w: fogOptics.Scattering);
        data.FogParams0 = new Vector4(x: kExp2, y: kHeight, z: fogHeight.BaseHeight, w: fogHeight.Strength);
        data.FogParams1 = new Vector4(x: fogOptics.DistanceWeight, y: fogOptics.HeightWeight, z: fogHeight.MaxDistance,
            w: 0.0f);

        ctx.UploadUniform(&data);
    }

    [SkipLocalsInit]
    private static void UploadDirLight(in UniformUploadContext ctx)
    {
        ref readonly var fogHeight = ref VisualManager.Illumination.DirectionalLight.Value;

        DirectionalLightUniform data;
        data.Direction = fogHeight.Direction.AsVector4();
        data.Diffuse = new Vector4(fogHeight.Diffuse, fogHeight.Intensity);
        data.Specular = new Vector4(fogHeight.Specular, 0.0f, 0.0f, 0.0f);

        ctx.UploadUniform(&data);
    }

    [SkipLocalsInit]
    private static void UploadPost(in UniformUploadContext ctx)
    {
        var post = VisualManager.PostEffect;
        ref readonly var grade = ref post.Grade.Value;
        ref readonly var wb = ref post.WhiteBalance.Value;
        ref readonly var bloom = ref post.Bloom.Value;
        ref readonly var fx = ref post.ImageFx.Value;

        PostFxUniform data;
        data.Grade = new Vector4(grade.Exposure, grade.Saturation, grade.Contrast, grade.Warmth);
        data.WhiteBalance = new Vector4(wb.Tint, wb.Strength, 0f, 0f);
        data.Bloom = new Vector4(bloom.Intensity, bloom.Threshold, bloom.Radius, 0f);
        data.Fx = new Vector4(fx.Vignette, fx.Grain, fx.Sharpen, fx.Rolloff);

        ctx.UploadUniform(&data);
    }
}