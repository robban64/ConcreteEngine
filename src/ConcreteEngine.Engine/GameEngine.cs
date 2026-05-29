using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Command;
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
    private readonly GraphicsRuntime _graphics;

    private readonly EngineWindow _window;
    private readonly EngineTickHub _tickHub;

    private readonly EngineCoreSystem _coreSystems;
    private readonly InputSystem _inputSystem;
    private readonly EngineRenderSystem _renderSystem;
    private readonly SceneSystem _sceneSystem;

    private readonly EngineGateway _gateway;
    private readonly EngineCommandQueue _commandQueues;

    private FrameStepper _systemStepper = new(8);
    private bool _isDisposed;

    internal GameEngine(
        EngineWindow window,
        GfxRuntimeBundle<GL> gfxBundle,
        EngineInputSource input,
        List<Func<GameScene>> sceneFactories
    )
    {
        _window = window;
        _graphics = gfxBundle.Graphics;

        var gpuCapabilities = _graphics.Initialize(gfxBundle.Config, out var version);

        EngineSettings.Current.LoadGraphicsSettings(version, gpuCapabilities);

        Ecs.Init();

        // systems
        var assets = new AssetSystem();
        _inputSystem = new InputSystem(input);
        _renderSystem = new EngineRenderSystem(_graphics, assets.Assets);
        _sceneSystem = new SceneSystem(sceneFactories, assets);

        _coreSystems = new EngineCoreSystem(_inputSystem, assets, _sceneSystem, _renderSystem);

        _gateway = new EngineGateway(window, _coreSystems);

        _commandQueues = new EngineCommandQueue(new EngineCommandContext
        {
            Assets = new AssetCommandSurface(assets), Renderer = new RenderCommandSurface()
        });

        _tickHub = new EngineTickHub(OnGameTick, _sceneSystem.GameSystem.UpdateSimulate, _gateway.UpdateDiagnostics,
            OnSystemTick);

        EngineSetupPipeline.Setup(new EngineSetupCtx
        {
            Graphics = _graphics,
            Window = _window,
            CommandQueue = _commandQueues,
            CoreSystem = _coreSystems,
            EngineGateway = _gateway,
            TickHub = _tickHub
        });
    }

    internal void RunSetup()
    {
        var runner = EngineSetupPipeline.Instance!;
        var isDone = runner.Run();
        EngineHost.IsSetupSimulation = runner.CurrentStep >= EngineSetupState.LoadEditor;

        _graphics.Gfx.Commands.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);
        if (!isDone) return;

        Console.WriteLine("Engine Setup Complete. Swapping to Game Loop.");
        Logger.LogString(LogScope.Engine, "Engine Setup Complete. Swapping to Game Loop.");
        runner.Teardown();

        _systemStepper.SetIntervalTicks(8, 8);
        OnSystemTick(0);
    }

    internal void Render(double delta)
    {
        var dt = (float)delta;
        _gateway.Metrics.StartCapture();

        // Update
        _inputSystem.Update(_window.Viewport.Position);
        _gateway.BeginFrame();

        _tickHub.Update(dt);
        _tickHub.AdvanceFrame(dt);

        // Draw
        Draw(dt);

        // Editor
        _inputSystem.EndFrame();

        _gateway.Metrics.EndCapture();
    }


    private void Draw(float dt)
    {
        var vp = _window.Viewport.Size;
        _graphics.BeginFrame(new GfxFrameArgs(dt, vp));
        _renderSystem.Render(dt, vp, _inputSystem.MouseState.ViewPos);
        _graphics.EndFrame();

        _gateway.RenderEditor(dt);
    }


    private void OnGameTick(float dt)
    {
        _renderSystem.BeforeUpdate();
        _sceneSystem.UpdateScene(dt);
        _renderSystem.AfterUpdate();

        _gateway.UpdateGameTick(dt);
    }


    private void OnSystemTick(float dt)
    {
        TerrainSystem.Instance.OnTick();

        if (_systemStepper.Tick() && _window.Refresh())
        {
            var command = new FboCommandRecord(CommandFboAction.ScreenSize, _window.Viewport.Size);
            _commandQueues.Enqueue(command);
        }

        if (_coreSystems.Assets.PendingAssetCount > 0)
            _coreSystems.Assets.ProcessPendingQueue();

        if (_commandQueues.QueuesCount > 0)
            _commandQueues.DrainDispatch();
    }

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
        _sceneSystem.Current?.Unload();
        _coreSystems.Assets.Shutdown();
        _coreSystems.GetSystem<EngineRenderSystem>().Shutdown();
        _graphics.Dispose();
    }
}