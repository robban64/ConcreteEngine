using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Render;

public sealed class RenderObjectManager
{
    internal readonly TerrainManager TerrainManager;
    internal readonly ParticleManager Particles;
    internal readonly AnimationTable Animations;

    internal RenderObjectManager(GraphicsRuntime graphics)
    {
        TerrainManager = new TerrainManager(graphics.Gfx);
        Particles = new ParticleManager(graphics.Gfx);
        Animations = new AnimationTable();
    }
}


public sealed unsafe class VisualUniformProcessor(GlobalVisualSettings visuals)
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
        var cameraTransforms = CameraManager.Instance.Transforms;
        var data = new CameraUniform(cameraTransforms.Translation, in cameraTransforms.FrameMatrices);
        ctx.UploadUniform(&data);
    }
    
    [SkipLocalsInit]
    public static void UploadLightView(UniformUploadContext ctx)
    {
        var cameraTransforms = CameraManager.Instance.Transforms;
        var data = new CameraUniform(cameraTransforms.Translation, in cameraTransforms.LightMatrices);
        ctx.UploadUniform(&data);
    }
    
    [SkipLocalsInit]
    public static void UploadShadow(UniformUploadContext ctx)
    {
        var shadow = GlobalVisualSettings.Instance.Shadow;

        ref readonly var proj = ref shadow.Projection.Value;
        ref readonly var vis =  ref shadow.Visuals.Value;

        var size = 1.0f / shadow.ShadowMapSize;

        ShadowUniform data;
        CameraManager.Instance.Transforms.LightMatrices.GetProjectionView(out data.LightViewProj);
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

public sealed class EngineRenderSystem : RenderSystem, IGameEngineSystem
{
    internal RenderProgram Program { get; }

    private readonly EngineWindow _window;
    private readonly FrameProcessor _frameProcessor;
    private readonly RenderDispatcher _renderDispatcher;
    
    private readonly GlobalVisualSettings _visualSettings;
    private readonly VisualUniformProcessor _uniformProcessor;

    private readonly CameraManager _cameraManager;
    private readonly RenderObjectManager _renderObjectManager;

    internal EngineRenderSystem(EngineWindow window, GraphicsRuntime graphics, MaterialStore materialStore)
    {
        _window = window;
        _cameraManager = CameraManager.Instance;
        _renderObjectManager = new RenderObjectManager(graphics);

        _renderDispatcher = new RenderDispatcher(Animations, Particles);
        _frameProcessor = new FrameProcessor(materialStore);

        _visualSettings = GlobalVisualSettings.Instance;
        _visualSettings.Shadow.ShadowMapSize = EngineSettings.Instance.Graphics.ShadowSize;
        _uniformProcessor = new VisualUniformProcessor(_visualSettings);

        Program = new RenderProgram(graphics, new UniformUploaderCallbacks
        {
            UploadMainView = VisualUniformProcessor.UploadMainView,
            UploadLightView = VisualUniformProcessor.UploadLightView,
            UploadShadow = VisualUniformProcessor.UploadShadow
        });

    }

    internal  TerrainManager Terrains => _renderObjectManager.TerrainManager;
    internal  ParticleManager Particles => _renderObjectManager.Particles;
    internal  AnimationTable Animations => _renderObjectManager.Animations;

    public override Terrain Terrain => Terrains.Terrain;
    public override int VisibleCount => _renderDispatcher.VisibleCount;
    public override ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();


    internal void Initialize(AssetStore assetStore, MaterialStore materialStore)
    {
        Animations.Setup(assetStore);
        _renderDispatcher.Attach(Program.UploadBuffers);
        _uniformProcessor.Attach(Program.UniformUploader);

        //
        var mat = materialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };
        DrawTagResolver.BoundsMaterial = mat.MaterialId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeforeUpdate()
    {
        _cameraManager.Camera.BeginUpdate(_window.Viewport.Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AfterUpdate()
    {
        _visualSettings.Ensure();
        _cameraManager.Update(_visualSettings);
        Terrains.Update();
    }


    internal void Render(float dt, Size2D viewportSize, Vector2 mousePos)
    {
        Program.PrepareFrame();
        
        if(_visualSettings.HasPendingFrameBufferResize)
            Program.ResizeFrameBuffers(viewportSize, _visualSettings.Shadow.ShadowMapSize);

        // frame update
        _cameraManager.UpdateFrameView(EngineTime.GameAlpha);
        _frameProcessor.SubmitMaterialData(Program);
        _frameProcessor.Execute(dt, EngineTime.GameAlpha);

        // process and upload draw commands
        _renderDispatcher.Execute();

        // prepare buffers
        Program.CollectDrawBuffers();

        // upload buffers to gpu
        _uniformProcessor.Upload(viewportSize, mousePos);
        Program.UploadUniforms();

        Program.Render();
        
        GlobalVisualSettings.Instance.ClearDirty();
    }

    public void Shutdown() => _renderDispatcher.Dispose();
}