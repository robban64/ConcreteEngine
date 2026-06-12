using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Processor;
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

    private readonly AnimationManager _animationManager;
    private readonly TerrainSystem _terrainSystem;
    private readonly ParticleSystem _particleSystem;

    private readonly MaterialProcessor _materialProcessor;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _cameraManager = CameraManager.Instance;
        _visualManager = VisualManager.Instance;
        _visualManager.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        Program = new RenderProgram(graphics, VisualUniformProcessor.MakeCallbacks());

        _terrainSystem = new TerrainSystem(graphics.Gfx);
        _particleSystem = new ParticleSystem(graphics.Gfx);
        _animationManager = AnimationManager.Instance;
        
        _renderDispatcher = new RenderDispatcher(_cameraManager, _animationManager, Program.UploadBuffers);
        _materialProcessor = new MaterialProcessor(Program);
    }

    public int VisibleCount => _renderDispatcher.VisibleCount;

    internal void Initialize()
    {
        _animationManager.Setup(AssetStore.Instance);

        //
        var boundsMaterial = AssetStore.Instance.CreateMaterial("EmptyMat", "EmptyMat1");
        boundsMaterial.State.DrawState =
            GfxDrawState.Set(GfxDrawFlags.Blend, GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2);
        boundsMaterial.State.PassFunctions = new GfxPassFunctions(BlendMode.Alpha);
        Material.BoundsMaterialId = boundsMaterial.MaterialId;
    }

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
        _animationManager.Simulate(dt);
        _particleSystem.Simulate(dt);
    }

    internal void Render(float dt, Size2D viewportSize)
    {
        Program.PrepareFrame();
        
        // frame update
        _cameraManager.CommitFrame(EngineTime.GameAlpha);

        // process and upload draw commands
        _particleSystem.Upload();
        _renderDispatcher.Prepare(_terrainSystem);
        _renderDispatcher.Execute();

        // prepare buffers
        Program.CollectDrawBuffers();

        // upload buffers to gpu
        VisualUniformProcessor.Upload(Program.GetUploadContext(), viewportSize, EngineInput.Mouse.ViewportPos);

        Program.UploadUniforms();
        Program.Render();
    }


    public void Dispose()
    {
        _renderDispatcher.Dispose();
        _particleSystem.Dispose();
        Program.Dispose();
    }
}