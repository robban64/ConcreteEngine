using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
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
using ConcreteEngine.Renderer;
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

    private FrameStepper _systemStepper = new(4);
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

        var version = _graphics.Initialize(gfxBundle.Config, out var caps);

        EngineSettings.Instance.LoadGraphicsSettings(version, in caps);

        // systems
        var assets = new AssetSystem();
        _inputSystem = new InputSystem(input);
        _renderSystem = new EngineRenderSystem(_graphics, assets.MaterialStore);
        _sceneSystem = new SceneSystem(sceneFactories, assets, _renderSystem);

        _coreSystems = new EngineCoreSystem(_inputSystem, assets, _sceneSystem, _renderSystem);

        _gateway = new EngineGateway(_coreSystems);

        _commandQueues = new EngineCommandQueue(new EngineCommandContext
        {
            Assets = new AssetCommandSurface(assets),
            Renderer = new RenderCommandSurface(VisualManager.Instance.VisualEnv)
        });

        _tickHub = new EngineTickHub(OnGameTick, OnSimulateTick, _gateway.UpdateDiagnostics, OnSystemTick);

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

        _graphics.Gfx.Commands.Clear(new GfxPassClear(Color.Black, ClearBufferFlag.ColorAndDepth));
        if (!isDone) return;

        Logger.LogString(LogScope.Engine, "Engine Setup Complete. Swapping to Game Loop.");
        runner.Teardown();
    }

    internal void Render(double delta)
    {
        var dt = (float)delta;
        _gateway.Metrics.StartCapture();

        // Update
        _inputSystem.Update(_window.OutputSize);
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
        var gfxArgs = new GfxFrameArgs(dt, _window.OutputSize);
        _graphics.BeginFrame(gfxArgs);
        _renderSystem.PrepareFrame(dt, _window, _inputSystem.MouseUv);
        _renderSystem.Render(dt);
        _graphics.EndFrame();

        _gateway.RenderEditor(dt, gfxArgs.OutputSize);
    }


    private void OnGameTick(float dt)
    {
        _renderSystem.BeforeUpdate(_window.OutputSize);
        _sceneSystem.UpdateScene(dt);
        _renderSystem.AfterUpdate();

        _gateway.UpdateGameTick(dt);
    }

    private void OnSimulateTick(float dt)
    {
        _sceneSystem.GameSystem.UpdateSimulate(dt);
    }

    private void OnSystemTick(float dt)
    {
        if (_systemStepper.Tick())
        {
            if (!_window.Refresh()) return;

            var size = _window.OutputSize;
            var command = new FboCommandRecord(CommandFboAction.RecreateScreenDependentFbo, size);
            _commandQueues.EnqueueDeferred(new EngineCommandPackage(command));

            _gateway.OnResized();
        }

        if (_coreSystems.AssetSystem.PendingAssetCount > 0)
            _coreSystems.AssetSystem.ProcessPendingQueue(EngineTime.GameTickId);

        if (_commandQueues.QueuesCount > 0)
        {
            _commandQueues.DrainMainCommands();
            _commandQueues.DrainDeferredCommands();
        }
    }

    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _gateway.Dispose();
        _sceneSystem.Current?.Unload();
        _coreSystems.AssetSystem.Shutdown();

        // _graphics?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;
        _gateway.Dispose();
        _coreSystems.AssetSystem.Shutdown();
        _graphics.Dispose();
    }
}