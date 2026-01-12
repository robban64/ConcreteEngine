using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.State;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Engine;

public sealed class GameEngine : IDisposable
{
    private readonly GraphicsRuntime _graphics;

    private readonly EngineWindow _window;
    private readonly EngineTickHub _tickHub;

    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;
    private readonly EngineRenderSystem _renderSystem;

    private readonly World _world;
    private readonly SceneSystem _sceneSystem;

    private readonly EngineGateway _gateway;
    private readonly EngineCommandQueue _commandQueues;

    private readonly EngineCommandContext _commandContext;


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

        Ecs.InitGameEcs();
        Ecs.InitRenderEcs();

        // systems
        _renderSystem = new EngineRenderSystem(_graphics);
        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();

        _world = new World(window, _assets, _renderSystem.Program.GetRenderParams());

        _sceneSystem = new SceneSystem(sceneFactories, _assets, _world);
        _coreSystems = new EngineCoreSystem(_inputSystem, _assets, _world, _sceneSystem);

        _gateway = new EngineGateway();
        _commandQueues = new EngineCommandQueue();

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
                Renderer = _renderSystem,
                Window = _window,
                CommandQueue = _commandQueues,
                SceneSystem = _sceneSystem,
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

        var outputSize = _window.OutputSize;

        var renderArgs = new RenderFrameArgs
        {
            Alpha = EngineTime.GameAlpha,
            DeltaTime = EngineTime.DeltaTime,
            MousePos = _inputSystem.MouseState.Position,
            OutputSize = outputSize,
            Rng = EngineTime.FrameRng,
            Time = EngineTime.Time
        };

        _graphics.BeginFrame(new GfxFrameArgs(dt, outputSize));
        _renderSystem.Render(in renderArgs);
        _graphics.EndFrame();

        _gateway.RenderEditor(dt, outputSize);

        _inputSystem.EndFrame();
        EngineMetricHub.Tick();
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
        _world.Update(dt, _window.OutputSize);

        _sceneSystem.UpdateScene(dt);

        _world.EndUpdate(_renderSystem.Program.RenderCamera);

        _sceneSystem.GameSystem.Update(dt);
    }

    private void OnSystemTick(float dt)
    {
        if (_systemStepper.Tick())
        {
            if (!_window.Refresh()) return;

            var command = new FboCommandRecord(CommandFboAction.RecreateScreenDependentFbo, _window.OutputSize);
            _commandQueues.EnqueueDeferred(new EngineCommandPackage(command));

            _gateway.OnResized();
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
        _sceneSystem.Current?.Unload();
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