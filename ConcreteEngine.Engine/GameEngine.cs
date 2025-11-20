#region

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
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Batching;
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

    private readonly UpdateFrameInfo _updateInfo;
    private readonly RenderEngineFrameInfo _renderFrameInfo;

    private readonly EngineTimeHub _timeHub;
    private readonly EngineGateway _engineGateway;
    private readonly EditorEngineQueue _editorQueues;

    private bool _isDisposed = false;

    private LinearStateMachine<EngineStateLevel> _stateMachine;

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
        _timeHub = new EngineTimeHub(GameTickUpdate, DebugTickUpdate);

        // systems

        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();

        _world = new World();
        _worldRenderer = new WorldRenderer(engineWindow, _graphics, _assets, _eventBus, _world.WorldRenderParams);
        _coreSystems = new EngineCoreSystem(_worldRenderer, _inputSystem, _assets);


        _stateMachine = new LinearStateMachine<EngineStateLevel>(Enum.GetValues<EngineStateLevel>());

        var internalInput = input.InputContext;
        _engineGateway =
            new EngineGateway(gfxBundle.Config.DriverContext, engineWindow.PlatformWindow, internalInput);
        _editorQueues = new EditorEngineQueue(_world, _worldRenderer, _assets);

        _updateInfo = new UpdateFrameInfo();
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

        EngineGateway.SetupLogger();
        EngineGateway.ToggleEngineLogger(true);
        EngineGateway.ToggleGfxLogger(true);
    }

    private void RegisterRenderer()
    {
        var builder = _worldRenderer.Initialize();
        _worldRenderer.SetupRenderer(builder);
    }

    internal void Render(float dt)
    {
        var alpha = _timeHub.Alpha;

        var frameStatus = _renderFrameInfo.BeginRenderFrame(dt, alpha, _window, _inputSystem.InputSource,
            out var frameInfo, out var runtimeParams);

        if (_sceneManager.Current is not { } scene)
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

        if (_timeHub.RenderTicker.TryProcessMetrics(dt))
            _engineGateway.RefreshMetrics();

        if (_timeHub.RenderTicker.TryProcessLoggers(dt))
            _engineGateway.DrainLogs();

        _worldRenderer.PreRender(beginStatus, in frameInfo, in runtimeParams, _world.Camera);
        _worldRenderer.ExecuteFrame(out var gfxFrameResult);
        _renderFrameInfo.EndRenderFrame(gfxFrameResult);

        _engineGateway.RenderEditor(in frameInfo);
    }

    internal void Update(float dt)
    {
        var outputSize = _window.OutputSize;
        _updateInfo.BeginUpdateFrame(dt, _window.WindowSize, outputSize);
        ref readonly var updateInfo = ref _updateInfo.UpdateTickInfo;
        
        _inputSystem.Update(!_engineGateway.BlockInput());

        if (_stateMachine.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            return;
        }   

        if (_assets.PendingAssetCount > 0)
        {
            _assets.ProcessPendingQueue(updateInfo.UpdateIndex);
        }

        if (_editorQueues.MainCommandCount > 0)
            _editorQueues.DrainMainCommands();

        if (_editorQueues.DeferredCommandCount > 0)
            _editorQueues.DrainDeferredCommands();
        
        _timeHub.AdvanceTick(dt);

        _world?.StartUpdate(outputSize,dt);
        _sceneManager.Current?.Update(in updateInfo);

    }

    private void GameTickUpdate(int tick, float fixedDt)
    {
        _updateInfo.UpdateTick(tick, fixedDt);

        if (_sceneManager.Current == null)
        {
            UpdateSceneTransitionIfNeeded();
            return;
        }

        _world.StartTick(fixedDt);
        
        _sceneManager.Current.UpdateTick(tick);
        
        _world.EndTick();
        
        UpdateSceneTransitionIfNeeded();
    }

    private void DebugTickUpdate(int tick, float tickDt)
    {
    }

    private void RunSetupStateMachine()
    {
        switch (_stateMachine.Current)
        {
            case EngineStateLevel.NotStarted:
                _stateMachine.Next();
                break;
            case EngineStateLevel.LoadingGraphics:
                StartAssetLoader();
                _stateMachine.Next();
                break;
            case EngineStateLevel.LoadingAssets:
                _stateMachine.Next(_assets.ProcessLoader(8));
                break;
            case EngineStateLevel.InitializeSystem:
                InitializeSystems();
                _stateMachine.Next();
                break;
            case EngineStateLevel.LoadScenes:
                _sceneManager.QueueSwitch(0);
                _stateMachine.Next();
                break;
        }
    }

    private void UpdateSceneTransitionIfNeeded()
    {
        if (!_sceneManager.HasPendingSwitch)
            return;

        _worldRenderer.AttachWorld(_world);
        _engineGateway.SetupEditor(_editorQueues, _world, _assets, _renderFrameInfo);

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