using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Metadata.Command;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.State;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Engine;

public sealed class GameEngine : IDisposable
{
    private readonly GraphicsRuntime _graphics;
    private readonly RenderEngine _renderer;

    private readonly EngineWindow _window;
    private readonly EngineTickHub _tickHub;

    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;

    private readonly World _world;
    private readonly SceneManager _sceneManager;

    private readonly EngineGateway _gateway;
    private readonly EngineCommandQueue _commandQueues;

    private readonly EngineCommandContext _commandContext;

    private FastRandom _rng = new(12323);

    private FrameStepper _systemStepper = new(4);

    private bool _isDisposed;

    private EngineSetupPipeline? _setupPipeline;

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
        PrimitiveMeshes.CreatePrimitives(_graphics.Gfx.Meshes);

        // systems
        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();

        _renderer = new RenderEngine(_graphics, PrimitiveMeshes.FsqQuad);

        _world = new World(window, _graphics, _renderer, _assets);
        _sceneManager = new SceneManager(sceneFactories, _assets, _world);

        _coreSystems = new EngineCoreSystem(_inputSystem, _assets, _world, _sceneManager);

        _commandQueues = new EngineCommandQueue();

        _gateway = new EngineGateway();

        // time
        _tickHub = new EngineTickHub(OnGameTick, _world.OnSimulationTick, _gateway.UpdateDiagnostics, OnSystemTick);

        _commandContext = new EngineCommandContext
        {
            Assets = new AssetCommandSurface(_assets), Renderer = new RenderCommandSurface(_world.WorldVisual)
        };

        _setupPipeline = new EngineSetupPipeline();
        EngineSetupBootstrapper.RegisterSteps(_setupPipeline,
            new EngineSetupCtx
            {
                Assets = _assets,
                Graphics = _graphics,
                Renderer = _renderer,
                Window = _window,
                CommandQueue = _commandQueues,
                SceneManager = _sceneManager,
                CoreSystem = _coreSystems,
                EngineGateway = _gateway,
                World = _world,
                InputSystem = _inputSystem
            });
    }

    internal void RunSetup(double deltaTime)
    {
        var isDone = _setupPipeline!.Run((float)deltaTime);
        EngineHost.IsSetupSimulation = _setupPipeline.CurrentStep >= EngineSetupState.LoadEditor;

        _graphics.Gfx.Commands.Clear(new GfxPassClear(Color.Black, ClearBufferFlag.ColorAndDepth));
        if (!isDone) return;

        Logger.LogString(LogScope.Engine, "Engine Setup Complete. Swapping to Game Loop.");
        _setupPipeline.Teardown();
        _setupPipeline = null;
        EngineHost.IsSetup = false;
        EngineHost.IsSetupSimulation = false;

        _inputSystem.ClearInputState();
        _tickHub.Reset();
    }

    internal void Render(double delta)
    {
        var dt = (float)delta;
        
        _tickHub.BeginFrame(dt);

        Size2D outputSize = _window.OutputSize, windowSize = _window.WindowSize;

        var mousePos = _inputSystem.MouseState.Position;

        var frameInfo = new FrameInfo(EngineTime.FrameId, dt, EngineTime.GameAlpha, outputSize);
        var runtimeParams = new RenderRuntimeParams(windowSize, mousePos, EngineTime.Time, _rng.NextFloat());

        _graphics.BeginFrame(new GfxFrameArgs(frameInfo.FrameId, dt, outputSize));
        _renderer.PrepareFrame(in frameInfo, in runtimeParams);

        _world.PreRender();
        _renderer.Render();

        _graphics.EndFrame();

        _gateway.RenderEditor(dt, outputSize);

        EngineMetricHub.Tick();
        
        _inputSystem.EndFrame();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update(double delta)
    {
        var dt = (float)delta;
        _inputSystem.Update();
        _tickHub.Update(dt);
    }

    private void OnGameTick(float dt)
    {

        _world.UpdateTick(dt, _window.OutputSize);

        _sceneManager.UpdateTick(dt);

        _world.EndUpdateTick(dt);
    }

    private void OnSystemTick(float dt)
    {
        if (_systemStepper.Tick())
        {
            if (!_window.Refresh()) return;

            var command = new FboCommandRecord(CommandFboAction.RecreateScreenDependentFbo, _window.OutputSize);
            _commandQueues.EnqueueDeferred(new EngineCommandPackage(command));
        }

        if (_assets.PendingAssetCount > 0)
            _assets.ProcessPendingQueue(EngineTime.GameTickId);

        if (_commandQueues.QueuesCount > 0)
        {
            _commandQueues.DrainMainCommands();
            _commandQueues.DrainDeferredCommands(_commandContext);
        }
    }

    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _gateway.Dispose();
        _sceneManager.Current?.Unload();
        _assets.Shutdown();
        // _graphics?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;
        _gateway.Dispose();
        _assets.Shutdown();
        //_graphics?.Dispose();
    }
}