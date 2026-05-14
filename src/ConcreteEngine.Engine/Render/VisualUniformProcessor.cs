using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;


public sealed unsafe class VisualUniformProcessor(VisualManager visuals)
{
    private  UniformUploadContext? _uniformUploader;

    public void Attach(UniformUploadContext uniformUploader) => _uniformUploader = uniformUploader;
    
    public void Upload(Size2D outputSize, Vector2 mouse)
    {
        if(_uniformUploader is null) return;

        UploadEngineUniformRecord(outputSize, mouse);
        
        if(!visuals.AnyWasDirty) return;
        
        if(visuals.Illumination.WasDirty) 
            UploadDirLight();
        
        if(visuals.Illumination.WasDirty || visuals.Environment.WasDirty) 
            UploadFrameUniformRecord();
        
        if(visuals.PostEffect.WasDirty)
            UploadPost();
    }

    [SkipLocalsInit]
    public static void UploadMainView(UniformUploadContext ctx)
    {
        var cameraTransforms = CameraManager.Instance.RenderTransforms;
        var data = new CameraUniform(cameraTransforms.Translation, in cameraTransforms.FrameMatrices);
        ctx.UploadUniform(&data);
    }
    
    [SkipLocalsInit]
    public static void UploadLightView(UniformUploadContext ctx)
    {
        var cameraTransforms = CameraManager.Instance.RenderTransforms;
        var data = new CameraUniform(cameraTransforms.Translation, in cameraTransforms.LightMatrices);
        ctx.UploadUniform(&data);
    }
    
    [SkipLocalsInit]
    public static void UploadShadow(UniformUploadContext ctx)
    {
        var shadow = VisualManager.Instance.Shadow;

        ref readonly var proj = ref shadow.Projection.Value;
        ref readonly var vis =  ref shadow.Visuals.Value;

        var size = 1.0f / shadow.ShadowMapSize;

        ShadowUniform data;
        CameraManager.Instance.RenderTransforms.LightMatrices.CalcProjectionView(out data.LightViewProj);
        data.ShadowParams0 = new Vector4(size, size, proj.ConstBias, proj.SlopeBias);
        data.ShadowParams1 = new Vector4(vis.Strength, vis.PcfRadius, 0.03f, proj.Distance);
        
        ctx.UploadUniform(&data);
    }
    
    [SkipLocalsInit]
    private void UploadEngineUniformRecord(Size2D outputSize, Vector2 mouse)
    {
        var data = new EngineUniformRecord(
            invResolution: new Vector2(1.0f / outputSize.Width, 1.0f / outputSize.Height),
            mouse: CoordinateMath.ToUvCoords(mouse, outputSize),
            deltaTime: EngineTime.DeltaTime,
            time: EngineTime.Time,
            random: EngineTime.FrameRng
        );

        _uniformUploader!.UploadUniform(&data);
    }

    [SkipLocalsInit]
    private void UploadFrameUniformRecord()
    {
        var env = visuals.Environment;

        ref readonly var fogHeight = ref env.FogHeight.Value;
        ref readonly var fogOptics =  ref env.FogOptics.Value;
        ref readonly var ambient = ref visuals.Illumination.Ambient.Value;

        float kExp2 = 1f / (fogHeight.Density * fogHeight.Density);
        float kHeight = 1f / MathF.Max(x: fogHeight.HeightFalloff, y: 1e-6f);

        FrameUniform data;
        data.Ambient = new Vector4(value: ambient.Ambient, w: ambient.Exposure);
        data.AmbientGround = new Vector4(value: ambient.AmbientGround, w: 0.0f);
        
        data.FogColor = new Vector4(value: fogOptics.Color, w: fogOptics.Scattering);
        data.FogParams0 = new Vector4(x: kExp2, y: kHeight, z: fogHeight.BaseHeight, w: fogHeight.Strength);
        data.FogParams1 = new Vector4(x: fogOptics.DistanceWeight, y: fogOptics.HeightWeight, z: fogHeight.MaxDistance, w: 0.0f);

        _uniformUploader!.UploadUniform(&data);
    }

    [SkipLocalsInit]
    private void UploadDirLight()
    {
        ref readonly var fogHeight = ref visuals.Illumination.DirectionalLight.Value;

        DirectionalLightUniform data;
        data.Direction = fogHeight.Direction.AsVector4();
        data.Diffuse = new Vector4(fogHeight.Diffuse, fogHeight.Intensity);
        data.Specular = new Vector4(fogHeight.Specular, 0.0f, 0.0f, 0.0f);

        _uniformUploader!.UploadUniform(&data);
    }

    [SkipLocalsInit]
    private void UploadPost()
    {
        var post = visuals.PostEffect;
        ref readonly var grade = ref post.Grade.Value;
        ref readonly var wb =  ref post.WhiteBalance.Value;
        ref readonly var bloom = ref post.Bloom.Value;
        ref readonly var fx = ref post.ImageFx.Value;

        PostFxUniform data;
        data.Grade = new Vector4(grade.Exposure, grade.Saturation, grade.Contrast, grade.Warmth);
        data.WhiteBalance = new Vector4(wb.Tint, wb.Strength, 0f, 0f);
        data.Bloom = new Vector4(bloom.Intensity, bloom.Threshold, bloom.Radius, 0f);
        data.Fx = new Vector4(fx.Vignette, fx.Grain, fx.Sharpen, fx.Rolloff);
        
        _uniformUploader!.UploadUniform(&data);
    }

}