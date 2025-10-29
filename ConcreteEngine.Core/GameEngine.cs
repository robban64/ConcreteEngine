#region

using ConcreteEngine.Common.Patterns;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Diagnostic;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Modules;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Core.World.Render;
using ConcreteEngine.Core.World.Render.Batching;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Renderer.State;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngine : IDisposable
{
    private readonly EngineWindow _window;
    private readonly GraphicsRuntime _graphics;

    private EngineEventBus _eventBus;

    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;
    private readonly WorldRenderer _worldRenderer;


    private readonly ModuleManager _modules;
    private readonly SceneManager _sceneManager;

    private readonly UpdateFrameInfo _updateInfo;
    private readonly RenderEngineFrameInfo _renderFrameInfo;

    private readonly EngineTimeHub _timeHub;

    private readonly EngineGateway _engineGateway;

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
        _timeHub = new EngineTimeHub(GameTickUpdate, DebugTickUpdate, OnGpuTickUpload, OnGpuTickDispose);

        // systems

        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();
        _worldRenderer = new WorldRenderer(engineWindow, _graphics, _assets, _eventBus);
        _coreSystems = new EngineCoreSystem(_worldRenderer, _inputSystem, _assets);


        _stateMachine = new LinearStateMachine<EngineStateLevel>(Enum.GetValues<EngineStateLevel>());

        var internalInput = input.InputContext;
        _engineGateway =
            new EngineGateway(gfxBundle.Config.DriverContext, engineWindow.PlatformWindow, internalInput);

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

        // prevent spam from first load. Move up to log startup issues
        GfxLog.Enabled = true;
    }

    private void RegisterRenderer()
    {
        var builder = _worldRenderer.Initialize((gfx, batchers) => { batchers.Register(new TerrainBatcher(gfx)); });

        _worldRenderer.SetupRenderer(builder);
    }

    internal void Render(float dt)
    {
        var alpha = _timeHub.Alpha;

        var frameStatus = _renderFrameInfo.BeginRenderFrame(dt, alpha, _window, _inputSystem.InputSource,
            out var frameInfo, out var runtimeParams);

        _timeHub.RenderFrame(dt);

        _engineGateway.Update(dt);

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

        _assets.UpdatePendingQueue(frameInfo.FrameIndex);
        _assets.ProcessPendingQueue(_worldRenderer);

        scene.BeforeRender(out var viewSnapshot);

        _worldRenderer.PreRender(beginStatus, in frameInfo, in runtimeParams, in viewSnapshot);
        _worldRenderer.ExecuteFrame(out var gfxFrameResult);
        _renderFrameInfo.EndRenderFrame(gfxFrameResult);

        _engineGateway.RenderMetricsUi();


        // _renderTime.TickOrRenderEffect();
        //_renderTime.TickOrGpuDispose();
        //_renderTime.TickOrGpuUpload();
    }

    private void OnGpuTickDispose(int tick)
    {
    }

    private void OnGpuTickUpload(int tick)
    {
    }

    internal void Update(float dt)
    {
        _updateInfo.BeginUpdateFrame(dt, _window.WindowSize, _window.OutputSize);

        if (_stateMachine.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            return;
        }

        _timeHub.UpdateFrame(dt);

        var updateInfo = _updateInfo.UpdateTickInfo;
        _sceneManager.Current?.Update(in updateInfo, _window.OutputSize);

        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _updateInfo.UpdateTick(tick);
        _inputSystem.Update(!_engineGateway.BlockInput());
        _sceneManager.Current?.UpdateTick(tick);
    }

    private void DebugTickUpdate(int tick)
    {
        if (!_engineGateway.Enabled) return;
        _engineGateway.RefreshMetrics();
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

        var sceneContext = new GameSceneContext(_coreSystems) { Modules = _modules };
        var builder = new GameSceneConfigBuilder(_modules);

        _sceneManager.ApplyPendingScene(sceneContext, builder, _worldRenderer, OnSceneBuild);

        _modules.Load(new GameModuleContext(sceneContext));
    }

    private void OnSceneBuild(SceneManager.SceneBuildResult result, WorldRenderer renderer)
    {
        _engineGateway.AttachDebugTools((World.World)result.Context.World, _assets, _renderFrameInfo);
        _engineGateway.RegisterCommands();
        _engineGateway.RegisterMetrics();
        _engineGateway.RefreshMetrics(true);

        renderer.AttachWorld((World.World)result.Context.World);
        foreach (var module in result.Modules) result.Context.Modules.AddModule(module());
    }

    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _sceneManager.Current?.Unload();
        _assets?.Shutdown();
        // _graphics?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;
        _assets?.Shutdown();
        //_graphics?.Dispose();
    }
}