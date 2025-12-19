using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Definitions;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Renderer.State;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Engine;

public sealed class GameEngine : IDisposable
{
    private readonly GraphicsRuntime _graphics;
    private readonly EngineWindow _window;
    private readonly EngineTimeHub _timeHub;
    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;

    private readonly World _world;
    private readonly SceneManager _sceneManager;

    private readonly EntityWorld _ecs;

    private readonly EngineGateway _engineGateway;
    private readonly EditorEngineQueue _editorQueues;

    private FastRandom _rng = new(12323);

    private EngineSetupStepper _setupStepper = new(8);

    private RenderFrameInfo _frameInfo;
    private RenderRuntimeParams _runtimeParams;
    private GfxFrameResult _gfxFrameResult;

    private bool _isDisposed;

    internal GameEngine(
        EngineWindow engineWindow,
        GfxRuntimeBundle<GL> gfxBundle,
        EngineInputSource input,
        List<Func<GameScene>> sceneFactories
    )
    {
        _window = engineWindow;
        _graphics = gfxBundle.Graphics;

        _graphics.Initialize(gfxBundle.Config);


        // time
        _timeHub = new EngineTimeHub(UpdateTick, SimulationTickUpdate, LogTickUpdate);

        // systems
        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();

        _ecs = new EntityWorld();
        _world = new World(engineWindow, _graphics, _assets, _ecs);
        _sceneManager = new SceneManager(sceneFactories, _assets, _world, _ecs);

        _coreSystems = new EngineCoreSystem(_world.Renderer, _inputSystem, _assets);

        var internalInput = input.InputContext;
        _engineGateway =
            new EngineGateway(gfxBundle.Config.DriverContext, engineWindow.PlatformWindow, internalInput);
        _editorQueues = new EditorEngineQueue(_world, _world.Renderer, _assets);
    }

    private void InitializeLogger()
    {
        EngineGateway.ToggleEngineLogger(true);
        EngineGateway.ToggleGfxLogger(true);
        EngineGateway.SetupLogger();
    }


    private void StartAssetLoader()
    {
        _assets.Initialize();
        _assets.StartLoader(_graphics.Gfx);
    }

    private void InitializeSystems()
    {
        _assets.FinishLoading();
        RegisterRenderer();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _coreSystems.Initialize();
    }

    private void RegisterRenderer()
    {
        var builder = _world.Renderer.StartBuilder();
        _world.Renderer.SetupRenderer(builder);
    }

    internal void Render(float dt)
    {
        var mousePos = _inputSystem.InputSourceImpl.MousePosition;
        var worldRender = _world.Renderer;

        _timeHub.UpdateFrame(dt);

        _window.OnFrameStart(out var outputSize, out var windowSize);
        var frameInfo = _frameInfo = new RenderFrameInfo(EngineTime.FrameIndex, dt, EngineTime.GameAlpha, outputSize);
        var runtimeParams = _runtimeParams =
            new RenderRuntimeParams(windowSize, mousePos, EngineTime.Time, _rng.NextFloat());

        if (_sceneManager.Current is null)
        {
            worldRender.RenderEmptyFrame(in frameInfo);
            return;
        }


        var beginStatus = _window.UpdateCheckResized() ? BeginFrameStatus.Resize : BeginFrameStatus.None;
        if (EngineTime.FrameIndex > 1 && beginStatus == BeginFrameStatus.Resize)
            _timeHub.Debounce(int.Min(60, (int)frameInfo.Fps));

        beginStatus = _timeHub.TryTriggerDebounceResize() ? BeginFrameStatus.Resize : BeginFrameStatus.None;


        worldRender.PreRender(beginStatus, frameInfo, runtimeParams);
        worldRender.ExecuteFrame(out _gfxFrameResult);

        if (_engineGateway.Active)
            _engineGateway.RenderEditor(in frameInfo, _gfxFrameResult);

        if (!_sceneManager.Enabled)
        {
            _graphics.Gfx.Commands.Clear(GfxPassClear.MakeColorDepthClear(Color4.Black));
        }

    }

    internal void Update(float dt)
    {
        EngineTime.UpdateIndex++;

        if (_setupStepper.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            if (_setupStepper.Current < EngineStateLevel.Warmup) return;
        }

        if (_assets.PendingAssetCount > 0)
            _assets.ProcessPendingQueue(EngineTime.UpdateIndex);

        if (_editorQueues.QueuesCount > 0)
        {
            _editorQueues.DrainMainCommands();
            _editorQueues.DrainDeferredCommands();
        }

        if (_engineGateway.Active)
            _inputSystem.Update(!_engineGateway.BlockInput());

        _timeHub.Accumulate(dt);
        _timeHub.Advance(dt);
    }

    private void UpdateTick(float dt)
    {
        _world.StartTick(_window.OutputSize);
        if(_setupStepper.Current == EngineStateLevel.Running)
            _sceneManager.UpdateTick(dt);
        _world.EndTick();
    }


    private void SimulationTickUpdate(float dt)
    {
        _world.OnSimulationTick(dt);
    }

    private void LogTickUpdate(float dt)
    {
        if (_engineGateway.Active)
            _engineGateway.UpdateDiagnostics(in _frameInfo, _gfxFrameResult);
    }

    private void RunSetupStateMachine()
    {
        switch (_setupStepper.Current)
        {
            case EngineStateLevel.NotStarted:
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadingGraphics:
                StartAssetLoader();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadingAssets:
                _setupStepper.Next(_assets.ProcessLoader(8));
                break;
            case EngineStateLevel.InitializeSystem:
                InitializeSystems();
                InitializeLogger();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadWorld:
                InitializeWorld();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadEditor:
                LoadScene();
                if (_sceneManager.Current == null) throw new InvalidOperationException();
                _engineGateway.SetupEditor(_editorQueues, _world, _assets);
                _setupStepper.Next();
                break;
            case EngineStateLevel.Warmup:
                var result = _setupStepper.Next(++_setupStepper.WarmupTick > 60);
                if (result == EngineStateLevel.Running)
                    _sceneManager.SetEnabled(true);
                break;
        }
    }

    private void InitializeWorld()
    {
        _sceneManager.QueueSwitch(0);
        _world.Initialize(_assets, _graphics.Gfx);
    }

    private void LoadScene()
    {
        if (!_sceneManager.HasPendingSwitch) return;
        var builder = new GameSceneConfigBuilder();
        _sceneManager.ApplyPendingScene(builder, _coreSystems);
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
        _assets.Shutdown();
        _engineGateway.Dispose();
        //_graphics?.Dispose();
    }
}