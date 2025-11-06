#region

using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Common.Patterns;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Editor;
using ConcreteEngine.Core.Editor.Data;
using ConcreteEngine.Core.Editor.Definitions;
using ConcreteEngine.Core.Editor.Diagnostics;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Modules;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Core.Time.Tickers;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Core.Worlds.Render;
using ConcreteEngine.Core.Worlds.Render.Batching;
using ConcreteEngine.Graphics;
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

    private readonly World _world;
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
        _engineGateway.ToggleEngineLogger(true);
        _engineGateway.ToggleGfxLogger(true);
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
        
        if(_timeHub.RenderTicker.TryProcessLoggers(dt))
            _engineGateway.DrainLogs();

        if (EngineCommandHandler.CommandQueueCount > 0) ProcessCommandQueue();
        if (_assets.PendingAssetCount > 0) ProcessPendingQueue(frameInfo.FrameIndex);

        _worldRenderer.PreRender(beginStatus, in frameInfo, in runtimeParams, _world.Camera);
        _worldRenderer.ExecuteFrame(out var gfxFrameResult);
        _renderFrameInfo.EndRenderFrame(gfxFrameResult);

        _engineGateway.RenderMetricsUi();
    }


    private void ProcessPendingQueue(long frameIndex)
    {
        _assets.UpdatePendingQueue(frameIndex);
        _assets.ProcessPendingQueue();
    }

    private void ProcessCommandQueue()
    {
        while (EngineCommandHandler.TryDequeueCommand(out var cmd))
        {
            try
            {
                ProcessRequest(cmd);
                Logger.LogString(LogScope.Engine, $"Command invoked: {cmd}");
            }
            catch (Exception ex)
            {
                var msg = $"Error while processing command {cmd}";
                var level = ErrorUtils.IsUserOrDataError(ex) ? LogLevel.Warn : LogLevel.Critical;
                Logger.LogString(LogScope.Engine, msg, level);
                Logger.LogString(LogScope.Engine, ex.Message, level);

                if (!ErrorUtils.IsUserOrDataError(ex) && ex is not InvalidOperationException { InnerException: null })
                    throw;
            }
        }
    }

    void ProcessRequest(EngineCommandRecord cmd)
    {
        if (cmd.Scope == EngineCommandScope.AssetCommand && cmd is AssetCommandRecord assetCmd)
            _assets.EnqueueRecreateShader(assetCmd.Name);
        else if (cmd.Scope == EngineCommandScope.RenderCommand && cmd is FboCommandRecord fboCmd)
            _worldRenderer.RecreateFrameBuffer(fboCmd);
    }

    internal void Update(float dt)
    {
        _updateInfo.BeginUpdateFrame(dt, _window.WindowSize, _window.OutputSize);
        var updateInfo = _updateInfo.UpdateTickInfo;

        if (_stateMachine.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            return;
        }

        _engineGateway.Update(dt);

        _timeHub.AdvanceTick(dt);


        _sceneManager.Current?.Update(in updateInfo, _window.OutputSize);
        
        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _world.UpdateTick(_window.WindowSize);
        _updateInfo.UpdateTick(tick);
        _inputSystem.Update(!_engineGateway.BlockInput());
        _sceneManager.Current?.UpdateTick(tick);
        _world.EndTick();
    }

    private void DebugTickUpdate(int tick)
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
        _engineGateway.SetupEditor(_world, _assets, _renderFrameInfo);

        var sceneContext = new GameSceneContext(_coreSystems, _world) { Modules = _modules };
        var builder = new GameSceneConfigBuilder(_modules);

        _sceneManager.ApplyPendingScene(sceneContext, builder, static (result) =>
        {
            for(int i = 0; i <  result.Modules.Count; i++)
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