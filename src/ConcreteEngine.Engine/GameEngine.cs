using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Engine.Gateway;
using ConcreteEngine.Engine.Gateway.Diagnostics;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
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

        EngineSettings.Instance.LoadGraphicsSettings(version, gpuCapabilities);

        // systems
        var assets = new AssetSystem();
        _inputSystem = new InputSystem(input);
        _renderSystem = new EngineRenderSystem(window, _graphics, assets.MaterialStore);
        _sceneSystem = new SceneSystem(sceneFactories, assets, _renderSystem);

        _coreSystems = new EngineCoreSystem(_inputSystem, assets, _sceneSystem, _renderSystem);

        _gateway = new EngineGateway(window, _coreSystems);

        _commandQueues = new EngineCommandQueue(new EngineCommandContext
        {
            Assets = new AssetCommandSurface(assets),
            Renderer = new RenderCommandSurface(VisualManager.Instance.VisualEnv)
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

        _graphics.Gfx.Commands.Clear(new GfxPassClear(Color32.Black, ClearBufferFlag.ColorAndDepth));
        if (!isDone) return;

        Console.WriteLine("Engine Setup Complete. Swapping to Game Loop.");
        Logger.LogString(LogScope.Engine, "Engine Setup Complete. Swapping to Game Loop.");
        runner.Teardown();
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
        _gateway.RenderEditor(dt);
        _graphics.EndFrame();
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
        if (_systemStepper.Tick())
        {
            if (!_window.Refresh()) return;

            //VisualManager.Instance.VisualEnv.SetScreenFboSize(_window.Viewport.Size);
            var command = new FboCommandRecord(CommandFboAction.ScreenDependentFbo, _window.Viewport.Size);
            _commandQueues.Enqueue(command);
        }

        if (_coreSystems.AssetSystem.PendingAssetCount > 0)
            _coreSystems.AssetSystem.ProcessPendingQueue(EngineTime.GameTickId);

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
        _coreSystems.AssetSystem.Shutdown();
        _coreSystems.GetSystem<EngineRenderSystem>().Shutdown();
        _graphics.Dispose();
    }
}