using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Gateway;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Engine;

public sealed class GameEngine : IDisposable
{
    private static bool _isDisposed;

    private readonly GraphicsRuntime _graphics;

    private readonly EngineTickHub _tickHub;

    private readonly AssetSystem _assetSystem;
    private readonly SceneSystem _sceneSystem;
    private readonly EngineRenderSystem _renderSystem;

    private readonly EngineCommandQueue _commandQueues;

    private readonly EngineGateway _gateway;

    private FrameStepper _systemStepper = new(8);

    internal GameEngine(GfxRuntimeBundle<GL> gfxBundle, List<Func<GameScene>> sceneFactories)
    {
        _graphics = gfxBundle.Graphics;

        var gpuCapabilities = _graphics.Initialize(gfxBundle.Config, out var version);
        EngineSettings.Current.LoadGraphicsSettings(version, gpuCapabilities);

        Ecs.Init();

        _assetSystem = new AssetSystem(gfxBundle.Graphics.Gfx);
        _renderSystem = new EngineRenderSystem(gfxBundle.Graphics);
        _sceneSystem = new SceneSystem(sceneFactories);

        _commandQueues = new EngineCommandQueue(new EngineCommandContext(_assetSystem));

        _gateway = new EngineGateway(_renderSystem.Program);

        _tickHub = new EngineTickHub(this);

        EngineSetupPipeline.Setup(new EngineSetupCtx
        {
            Graphics = _graphics,
            EngineGateway = _gateway,
            TickHub = _tickHub,
            Assets = _assetSystem,
            Renderer = _renderSystem,
            SceneSystem = _sceneSystem,
            CommandQueue = _commandQueues
        });
    }

    internal void RunSetup()
    {
        var runner = EngineSetupPipeline.Current!;
        var isDone = runner.Run();
        EngineHost.IsSetupSimulation = runner.ActiveStep >= EngineSetupState.LoadEditor;

        _graphics.Gfx.Commands.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);
        if (!isDone) return;

        Logger.LogString(LogScope.Engine, "Engine Setup: Complete. Swapping to Game Loop.");

        Console.WriteLine("Engine Setup: Complete. Swapping to Game Loop.");
        runner.Teardown();
        Console.WriteLine("Engine Setup: Tear down complete");

        OnSystemTick(0);
        Console.WriteLine($"Fragmentation: {GC.GetGCMemoryInfo().FragmentedBytes}");
        Console.WriteLine($"Total Alloc: {GC.GetTotalAllocatedBytes() / 1024.0 / 1024.0:F2}");
    }

    internal void Render(float dt)
    {
        _gateway.Metrics.StartCapture();

        // Update
        EngineInput.Update();
        _gateway.BeginFrame();

        _tickHub.Update(dt);
        _tickHub.AdvanceFrame(dt);

        // Draw
        Draw(dt);

        EngineInput.Keyboard.ClearKeys();

        _gateway.Metrics.EndCapture();
    }


    private void Draw(float dt)
    {
        _graphics.BeginFrame(new GfxFrameArgs(dt, EngineWindow.Viewport.Size));
        _renderSystem.Render(dt);
        _graphics.EndFrame();

        _gateway.RenderEditor(dt);
    }

    internal void OnGameTick(float dt)
    {
        CameraManager.Instance.BeginUpdate();

        _sceneSystem.UpdateScene(dt);
        _gateway.UpdateGameTick(dt);
        _renderSystem.AfterUpdate();
    }

    internal void OnSimulateTick(float dt) => _renderSystem.OnSimulate(dt);

    internal void OnSystemTick(float dt)
    {
        var windowResized = _systemStepper.Tick() && EngineWindow.Commit();
        if(dt == 0)Console.WriteLine("_renderSystem.OnSystemTick");
        _renderSystem.OnSystemTick(windowResized);
        if(dt == 0)Console.WriteLine("_assetSystem.PendingAssetCount");

        if (_assetSystem.PendingAssetCount > 0)
            _assetSystem.ProcessPendingQueue();

        if (_commandQueues.QueuesCount > 0)
            _commandQueues.DrainDispatch();
    }

    internal void OnDiagnosticTick(float dt) => _gateway.UpdateDiagnostics(dt);

    internal void Close()
    {
        if (_isDisposed) return;
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        Cleanup();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;
        Cleanup();
    }

    private void Cleanup()
    {
        _gateway.Dispose();
        _sceneSystem.Shutdown();
        _renderSystem.Dispose();
        _assetSystem.Shutdown();

        EngineInput.Detach();
        _graphics.Dispose();
    }
}