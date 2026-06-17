using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem : IDisposable
{
    internal RenderProgram Program { get; }

    private readonly RenderDispatcher _renderDispatcher;

    private readonly CameraManager _cameraManager;
    private readonly VisualManager _visualManager;

    private readonly TerrainSystem _terrainSystem;
    private readonly ParticleSystem _particleSystem;
    private readonly AnimationSystem _animationSystem;

    private readonly MaterialProcessor _materialProcessor;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _cameraManager = CameraManager.Instance;
        _visualManager = VisualManager.Instance;
        _visualManager.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        Program = new RenderProgram(graphics, VisualUniformProcessor.MakeCallbacks());

        _terrainSystem = new TerrainSystem(graphics.Gfx);
        _particleSystem = new ParticleSystem(graphics.Gfx);
        _animationSystem = new AnimationSystem(AnimationManager.Instance, Program.UploadBuffers.Skinning);
        
        _renderDispatcher = new RenderDispatcher(_cameraManager,_terrainSystem, Program.UploadBuffers);
        _materialProcessor = new MaterialProcessor(Program);
    }

    public int VisibleCount => _renderDispatcher.VisibleEntities;

    internal void Initialize() { }

    internal void AfterUpdate()
    {
        _visualManager.Ensure();
        _cameraManager.CommitUpdate(_visualManager);
        _materialProcessor.Commit();
    }

    internal void OnSystemTick(bool screenResize)
    {
        _particleSystem.Commit();
        _terrainSystem.Commit();

        if (screenResize)
        {
            Logger.LogString(LogScope.Engine, "Recreating screen framebuffers");
            Program.ResizeScreenFrameBuffers(EngineWindow.Viewport.Size);
        }

        if (_visualManager.CommitShadowSize())
        {
            Logger.LogString(LogScope.Engine, "Recreating shadow framebuffers");
            Program.ResizeShadowFrameBuffers(_visualManager.Shadow.ShadowMapSize);
        }
    }

    internal void OnSimulate(float dt)
    {
        _animationSystem.Simulate(dt);
        _particleSystem.Simulate(dt);
    }

    internal void Render(float dt)
    {
        Program.PrepareFrame();
        
        // frame update
        _cameraManager.CommitFrame(EngineTime.GameAlpha);

        // process and upload draw commands
        _renderDispatcher.CullEntities();
        _particleSystem.Execute();
        _animationSystem.Execute();
        _renderDispatcher.Execute();

        // prepare buffers
        Program.CollectDrawBuffers();

        // upload buffers to gpu
        VisualUniformProcessor.Upload(Program.GetUploadContext());

        Program.UploadUniforms();
        Program.Render();
    }


    public void Dispose()
    {
        _particleSystem.Dispose();
        _animationSystem.Dispose();
        Program.Dispose();
    }
}