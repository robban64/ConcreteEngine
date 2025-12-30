using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Metadata.Command;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.State;
using Silk.NET.OpenGL;
using Shader = ConcreteEngine.Engine.Assets.Shaders.Shader;

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

    private readonly EngineGateway _engineGateway;
    private readonly EngineCommandQueue _commandQueues;

    private FastRandom _rng = new(12323);

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

        var driver = gfxBundle.Config.DriverContext;
        _engineGateway = new EngineGateway(new EditorPortalArgs(driver, window.PlatformWindow, input.InputContext));
        _commandQueues = new EngineCommandQueue(_world, _assets);

        // time
        _tickHub = new EngineTickHub(OnGameTick, OnEnvironmentTick, OnDiagnosticTick, OnSystemTick, OnRender);

        _setupPipeline = new EngineSetupPipeline();
         EngineSetupBootstrapper.RegisterSteps(_setupPipeline, GetStartupContext());

        _tickHub.StartSetup(RunSetup);
    }

    private void RunSetup(float deltaTime)
    {
        bool isDone = _setupPipeline!.Run(deltaTime);

        _graphics.Gfx.Commands.Clear(new GfxPassClear(Color.Black, ClearBufferFlag.ColorAndDepth));
        if (!isDone) return;

        Logger.LogString(LogScope.Engine, "Engine Setup Complete. Swapping to Game Loop.");
        _tickHub.FinishSetup();
        _setupPipeline = null;
    }


    private void OnRender(float dt)
    {
        var mousePos = _inputSystem.InputSource.MousePosition;
        var frameInfo = new FrameInfo(EngineTime.FrameId, dt, EngineTime.GameAlpha, _window.OutputSize);
        var runtimeParams = new RenderRuntimeParams(_window.WindowSize, mousePos, EngineTime.Time, _rng.NextFloat());

        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());
        _renderer.PrepareFrame(in frameInfo, in runtimeParams);

        _world.PreRender();
        _renderer.Render();

        _graphics.EndFrame();

        _engineGateway.RenderEditor(dt);

        EngineMetricHub.Tick();
    }

    internal void Update(double delta)
    {
        float dt = !_tickHub.IsSetup  ? (float)delta : 0;
        _tickHub.Update(dt);
    }

    internal void Render(double delta)
    {
        float dt = (float)delta;
        _tickHub.BeginFrame(dt);
    }

    private void OnGameTick(float dt)
    {
        EngineTime.GameTickId++;

        if (_assets.PendingAssetCount > 0)
            _assets.ProcessPendingQueue(EngineTime.GameTickId);

        if (_commandQueues.QueuesCount > 0)
        {
            _commandQueues.DrainMainCommands();
            _commandQueues.DrainDeferredCommands();
        }

        if (_engineGateway.Active)
            _inputSystem.Update(!_engineGateway.BlockInput());

        _world.UpdateTick(dt, _window.OutputSize);

        _sceneManager.UpdateTick(dt);

        _world.EndUpdateTick(dt);
    }


    private void OnEnvironmentTick(float dt) => _world.OnSimulationTick(dt);

    private void OnDiagnosticTick(float dt)
    {
        _engineGateway.UpdateDiagnostics();
    }

    private void OnSystemTick(float dt)
    {
        var pendingResize = _window.Refresh();
        if (pendingResize)
        {
            var command = new RenderCommandRecord(CommandRenderAction.RecreateScreenDependentFbo, _window.OutputSize);
            _commandQueues.EnqueueDeferred(new EngineCommandPackage(command));
        }
    }

    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _engineGateway.Dispose();
        _sceneManager.Current?.Unload();
        _assets.Shutdown();
        // _graphics?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;
        _engineGateway.Dispose();
        _assets.Shutdown();
        //_graphics?.Dispose();
    }

    private FullSetupCtx GetStartupContext() =>
        new()
        {
            Assets = _assets,
            Graphics = _graphics,
            Renderer = _renderer,
            Window = _window,
            CommandQueue = _commandQueues,
            SceneManager = _sceneManager,
            CoreSystem = _coreSystems,
            EngineGateway = _engineGateway,
            World = _world,
        };
}