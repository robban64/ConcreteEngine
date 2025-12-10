#region

using System.Diagnostics;
using ConcreteEngine.Common.Patterns;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Definitions;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Time.Tickers;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.State;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Engine;

public sealed class GameEngine : IDisposable
{
    private readonly EngineWindow _window;
    private readonly GraphicsRuntime _graphics;

    private EngineEventBus _eventBus;

    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;

    private readonly World _world;
    private readonly WorldRenderer _worldRenderer;


    private readonly ModuleManager _modules;
    private readonly SceneManager _sceneManager;

    private readonly EngineTickerInfo _updateInfo;
    private readonly RenderEngineFrameInfo _renderFrameInfo;

    private readonly EngineTimeHub _timeHub;
    private readonly EngineGateway _engineGateway;
    private readonly EditorEngineQueue _editorQueues;

    private bool _isDisposed = false;

    private EngineSetupStepper _setupStepper = new(7);

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

        _sceneManager = new SceneManager(sceneFactories);

        _modules = new ModuleManager();

        // time
        _timeHub = new EngineTimeHub(UpdateTick, SimulationTickUpdate, LogTickUpdate);

        // systems

        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();

        _world = new World();
        _worldRenderer = new WorldRenderer(engineWindow, _graphics, _assets, _eventBus!, _world.WorldRenderParams);
        _coreSystems = new EngineCoreSystem(_worldRenderer, _inputSystem, _assets);

        var internalInput = input.InputContext;
        _engineGateway =
            new EngineGateway(gfxBundle.Config.DriverContext, engineWindow.PlatformWindow, internalInput);
        _editorQueues = new EditorEngineQueue(_world, _worldRenderer, _assets);

        _updateInfo = new EngineTickerInfo();
        _renderFrameInfo = new RenderEngineFrameInfo(_window.OutputSize);
    }


    private void StartAssetLoader()
    {
        _assets.Initialize();
        _assets.StartLoader(_graphics.Gfx);
    }

    private void InitializeSystems()
    {
        _assets.FinishLoading();
        _coreSystems.Initialize();
        RegisterRenderer();

        EngineGateway.ToggleEngineLogger(true);
        EngineGateway.ToggleGfxLogger(true);
        EngineGateway.SetupLogger();
    }

    private void RegisterRenderer()
    {
        var builder = _worldRenderer.Initialize();
        _worldRenderer.SetupRenderer(builder);
    }

    internal void Render(float dt)
    {
        float alpha = EngineTime.GameAlpha = _timeHub.GetGameAlpha();
        EngineTime.SimulationAlpha = _timeHub.GetSimulationAlpha();

        var frameStatus = _renderFrameInfo.BeginRenderFrame(dt, alpha, _window, _inputSystem.InputSource,
            out var frameInfo, out var runtimeParams);

        if (_sceneManager.Current is null)
        {
            _worldRenderer.RenderEmptyFrame(in frameInfo);
            return;
        }

        if (_renderFrameInfo.FrameIndex > 1 && frameStatus == RenderEngineFrameInfo.BeginFrameStatus.Resize)
        {
            _timeHub.DebounceTicker ??= new DebounceTicker(30);
        }

        var beginStatus = BeginFrameStatus.None;
        if (_timeHub.DebounceTicker?.Tick() ?? false)
        {
            _timeHub.DebounceTicker = null;
            beginStatus = BeginFrameStatus.Resize;
        }


        _world.StartRenderFrame(alpha);
        _worldRenderer.PreRender(beginStatus, in frameInfo, in runtimeParams, _world.Camera);
        _worldRenderer.ExecuteFrame(out var gfxFrameResult);
        _renderFrameInfo.EndRenderFrame(gfxFrameResult);

        if (_engineGateway.Active)
            _engineGateway.RenderEditor(in frameInfo);
        
    }

    internal void Update(float dt)
    {
        _updateInfo.BeginUpdateFrame(dt, _window.WindowSize);
        var updateInfo = _updateInfo.UpdateTickInfo;
        if (_setupStepper.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            return;
        }

        if (_assets.PendingAssetCount > 0)
            _assets.ProcessPendingQueue(updateInfo.UpdateIndex);

        if (_editorQueues.MainCommandCount > 0)
            _editorQueues.DrainMainCommands();

        if (_editorQueues.DeferredCommandCount > 0)
            _editorQueues.DrainDeferredCommands();
        
        if (_engineGateway.Active)
            _inputSystem.Update(!_engineGateway.BlockInput());

        _timeHub.Advance(dt);
    }

    private void UpdateTick(float dt)
    {
        _engineGateway.UpdateEditorData();

        _world.StartTick(_window.OutputSize, dt, _renderFrameInfo.Time);

        _sceneManager.Current?.UpdateTick(dt);

        _world.EndTick(_worldRenderer.RenderCamera);

    }


    private void SimulationTickUpdate(float dt)
    {
        _world.OnSimulationTick(dt);
    }

    private void LogTickUpdate(float dt)
    {
        if (_engineGateway.Active)
            _engineGateway.UpdateDiagnostics(dt);
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
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadScenes:
                _sceneManager.QueueSwitch(0);
                UpdateSceneTransitionIfNeeded();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadEditor:
                if (_sceneManager.Current == null) throw new InvalidOperationException();
                _engineGateway.SetupEditor(_editorQueues, _world, _assets, _renderFrameInfo);
                _setupStepper.Next();
                break;
        }
    }

    private void UpdateSceneTransitionIfNeeded()
    {
        if (!_sceneManager.HasPendingSwitch)
            return;

        _worldRenderer.AttachWorld(_world);

        var sceneContext = new GameSceneContext(_coreSystems, _world) { Modules = _modules };
        var builder = new GameSceneConfigBuilder(_modules);

        _sceneManager.ApplyPendingScene(sceneContext, builder, static (result) =>
        {
            for (int i = 0; i < result.Modules.Count; i++)
                result.Context.Modules.AddModule(result.Modules[i]());
        });

        _modules.Load(new GameModuleContext(sceneContext));
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