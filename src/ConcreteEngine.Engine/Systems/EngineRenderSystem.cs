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
    private readonly RenderDispatcher _renderDispatcher;

    private readonly CameraManager _cameraManager;
    private readonly VisualManager _visualManager;

    private readonly TerrainSystem _terrainSystem;
    private readonly ParticleSystem _particleSystem;
    private readonly AnimationSystem _animationSystem;

    private readonly MaterialSystem _materialSystem;

    private readonly RenderRegistry _registry;
    private readonly RenderPassPipeline _passPipeline;
    private readonly DrawCommandPipeline _drawPipeline;


    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _cameraManager = CameraManager.Instance;
        _visualManager = VisualManager.Instance;
        _visualManager.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        _materialSystem = new MaterialSystem();
        _terrainSystem = new TerrainSystem(graphics.Gfx);
        _particleSystem = new ParticleSystem(graphics.Gfx);
        _animationSystem = new AnimationSystem(AnimationManager.Instance);

        _registry = new RenderRegistry(graphics.Gfx);
        _drawPipeline = new DrawCommandPipeline(graphics.Gfx, _animationSystem, _materialSystem);
        _passPipeline = new RenderPassPipeline(graphics.Gfx, _drawPipeline.DrawCmd,_registry);

        _renderDispatcher = new RenderDispatcher(_cameraManager.Frustum);
        
        VisualSystem.Create(graphics.Gfx.Buffers);

    }

    public int VisibleCount => _renderDispatcher.VisibleCount;

    internal void Init()
    {
        RegisterCoreShaders(AssetManager.Assets);
        PassPipeline3D.RegisterFrameBuffers(_registry);
        PassPipeline3D.RegisterPassPipeline(_passPipeline);
        
        VisualSystem.Instance.UploadPointLight();
        _renderDispatcher.Setup();
    }

    internal void AfterUpdate()
    {
        _visualManager.Ensure();
        _cameraManager.CommitUpdate(_visualManager);
        _materialSystem.Commit();
    }

    internal void OnSystemTick(bool screenResize)
    {
        _particleSystem.Commit();
        _terrainSystem.Commit();

        if (screenResize)
        {
            Logger.Log(LogScope.Engine, "Recreating screen framebuffers");
            _registry.RecreateScreenDependentFbo(EngineWindow.Viewport.Size);
            _cameraManager.Camera.SetAspectRatio(EngineWindow.AspectRatio);
        }

        if (_visualManager.CommitShadowSize())
        {
            Logger.Log(LogScope.Engine, "Recreating shadow framebuffers");
            var size = new Size2D(_visualManager.Shadow.ShadowMapSize);
            _registry.RecreateFixedFrameBuffer<ShadowTarget>(FboVariant.V0, size);
        }
    }

    internal void OnSimulate(float dt)
    {
        _animationSystem.Simulate(dt);
        _particleSystem.Simulate(dt);
    }

    public void Render(float alpha)
    {
        _animationSystem.ResetFrame();
        _passPipeline.ResetFrame();
        _drawPipeline.ResetFrame();

        // frame update
        _cameraManager.CommitFrame(alpha);

        // process and upload draw commands
        _renderDispatcher.Execute();
        
        _particleSystem.Execute();
        _animationSystem.Execute(alpha);

        // prepare buffers
        _drawPipeline.StageCommands(_renderDispatcher);

        Execute();
    }
    
    public void Execute()
    {
        while (_passPipeline.NextPass(out var nextPassId, out var passAction))
        {
            if (passAction == NextPassAction.Skip) continue;
            
            var passResult = _passPipeline.ApplyPass();

            if(passResult.Op is PassOp.Draw)
                _drawPipeline.ExecuteDrawPass(nextPassId, _renderDispatcher.VisibleEntities);

            _passPipeline.ApplyAfterPass();
        }
    }


    public void Dispose()
    {
        _renderDispatcher.Dispose();
        _particleSystem.Dispose();
        _animationSystem.Dispose();
        _materialSystem.Dispose();
    }
    
    private static void RegisterCoreShaders(AssetStore store)
    {
        RenderRegistry.DepthShader = store.GetByName<Shader>("Depth").GfxId;
        RenderRegistry.ColorFilterShader = store.GetByName<Shader>("ColorFilter").GfxId;
        RenderRegistry.CompositeShader = store.GetByName<Shader>("Composite").GfxId;
        RenderRegistry.PresentShader = store.GetByName<Shader>("Present").GfxId;
        RenderRegistry.HighlightShader = store.GetByName<Shader>("Highlight").GfxId;
        RenderRegistry.BoundingBoxShader = store.GetByName<Shader>("BoundingBox").GfxId;
    }

}