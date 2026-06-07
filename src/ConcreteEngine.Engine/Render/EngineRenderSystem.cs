using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem : IDisposable
{
    internal RenderProgram Program { get; }

    private readonly CameraManager _cameraManager;
    private readonly VisualManager _visualManager;

    private readonly MaterialProcessor _materialProcessor;
    private readonly ParticleSystem _particleSystem;
    private readonly RenderDispatcher _renderDispatcher;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _cameraManager = CameraManager.Instance;
        _visualManager = VisualManager.Instance;
        _visualManager.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        Program = new RenderProgram(graphics, VisualUniformProcessor.MakeCallbacks());

        TerrainSystem.Make(graphics.Gfx);
        _particleSystem = new ParticleSystem(graphics.Gfx);
        var animations = AnimationTable.Make();

        _renderDispatcher = new RenderDispatcher(animations, _particleSystem);
        _materialProcessor = new MaterialProcessor(Program);
    }

    public int VisibleCount => _renderDispatcher.VisibleCount;
    public ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();

    internal void Initialize(AssetStore assetStore)
    {
        AnimationTable.Instance.Setup(assetStore);
        _renderDispatcher.Attach(Program.UploadBuffers);

        //
        var boundsMaterial = assetStore.CreateMaterial("EmptyMat", "EmptyMat1");
        boundsMaterial.State.DrawState =
            GfxDrawState.Set(GfxDrawFlags.Blend, GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2);
        boundsMaterial.State.PassFunctions = new GfxPassFunctions(BlendMode.Alpha);
        DrawTagProcessor.BoundsMaterial = boundsMaterial.MaterialId;
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
        TerrainSystem.Instance.Commit();

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
        _particleSystem.Simulate(dt);
    }

    internal void Render(float dt, Size2D viewportSize, Vector2 mousePos)
    {
        Program.PrepareFrame();
        
        // frame update
        _cameraManager.CommitFrame(EngineTime.GameAlpha);

        // process and upload draw commands
        _renderDispatcher.Execute();

        // prepare buffers
        Program.CollectDrawBuffers();

        // upload buffers to gpu
        VisualUniformProcessor.Upload(Program.GetUploadContext(), viewportSize, mousePos);

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