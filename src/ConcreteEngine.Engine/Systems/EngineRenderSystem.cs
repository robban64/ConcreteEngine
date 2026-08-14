using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Systems;

public sealed class EngineRenderSystem : IDisposable
{
    private readonly RenderResolver _resolver;

    private readonly MaterialSystem _materialSystem;
    private readonly TerrainSystem _terrainSystem;
    private readonly ParticleSystem _particleSystem;
    private readonly AnimationSystem _animationSystem;

    private readonly DrawCommandPipeline _drawPipeline;


    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _ = CameraManager.Instance;
        _ = VisualManager.Instance;
        VisualManager.Instance.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        RenderRegistry.Create(graphics.Gfx);
        _materialSystem = new MaterialSystem();
        _terrainSystem = new TerrainSystem(graphics.Gfx);
        _particleSystem = new ParticleSystem(graphics.Gfx);
        _animationSystem = new AnimationSystem(AnimationManager.Instance);
        
        _resolver = new RenderResolver(CameraManager.Instance.Frustum);

        _drawPipeline = new DrawCommandPipeline(graphics.Gfx, _animationSystem, _materialSystem);

        VisualSystem.Create(graphics.Gfx.Buffers);
    }

    internal void Init()
    {
        RegisterCoreShaders(AssetManager.Assets);
        PassPipeline.RegisterFrameBuffers();
        PassPipeline.RegisterPassPipeline();
        VisualSystem.Instance.UploadPointLight();
    }

    internal void AfterUpdate()
    {
        VisualManager.Instance.Ensure();
        CameraManager.Instance.CommitUpdate();
        _materialSystem.Commit();
    }

    internal void OnSystemTick(bool screenResize)
    {
        _particleSystem.Commit();
        _terrainSystem.Commit();

        if (screenResize)
        {
            Logger.Log(LogScope.Engine, "Recreating screen framebuffers");
            RenderRegistry.Instance.RecreateScreenDependentFbo(EngineWindow.Viewport.Size);
            CameraManager.Instance.Camera.SetAspectRatio(EngineWindow.AspectRatio);
        }

        if (VisualManager.Instance.CommitShadowSize())
        {
            Logger.Log(LogScope.Engine, "Recreating shadow framebuffers");
            var size = new Size2D(VisualManager.Instance.Shadow.ShadowMapSize);
            RenderRegistry.Instance.RecreateFixedFrameBuffer<ShadowTarget>(FboVariant.V0, size);
        }
    }

    internal void OnSimulate(float dt)
    {
        _animationSystem.Simulate(dt);
        _particleSystem.Simulate(dt);
    }

    public void PrepareRenderer(float alpha)
    {
        RenderContext.ResetContext();
        _animationSystem.ResetFrame();
        _drawPipeline.ResetFrame();

        // frame update
        CameraManager.Instance.CommitFrame(alpha);
        VisualSystem.Instance.UploadUniforms();

        // process and upload draw commands
        _resolver.Execute();
        _particleSystem.Execute();
        _animationSystem.Execute(alpha);

        // prepare buffers
        VisualSystem.Instance.UploadUniformBuffers(_resolver, _materialSystem, _animationSystem);

        _drawPipeline.ReadyDrawCommands(_resolver.DrawIndices);
    }

    public static AvgFrameTimer avg;
    public void ExecuteRenderPipeline()
    {
        var length = RenderRegistry.PassCount;
        for (var i = 0; i < length; ++i)
        {
            _drawPipeline.RunPass(new PassId(i));
        }

        if (avg.EndSample() > 80 * 8) avg.ResetAndPrint();
    }


    public void Dispose()
    {
        _resolver.Dispose();
        _particleSystem.Dispose();
        _animationSystem.Dispose();
        _materialSystem.Dispose();
    }

    private static void RegisterCoreShaders(AssetStore store)
    {
        RenderStore.DepthShader = store.GetByName<Shader>("Depth").GfxId;
        RenderStore.ColorFilterShader = store.GetByName<Shader>("ColorFilter").GfxId;
        RenderStore.CompositeShader = store.GetByName<Shader>("Composite").GfxId;
        RenderStore.PresentShader = store.GetByName<Shader>("Present").GfxId;
        RenderStore.HighlightShader = store.GetByName<Shader>("Highlight").GfxId;
        RenderStore.BoundingBoxShader = store.GetByName<Shader>("BoundingBox").GfxId;
    }
}