#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Patterns;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Interface;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.RenderingSystem.Batching;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Modules;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.State;
using Silk.NET.Input;
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
    private readonly RenderingSystem.EngineRenderSystem _engineRenderSystem;


    private readonly ModuleManager _modules;
    private readonly SceneManager _sceneManager;

    private readonly UpdateFrameInfo _updateInfo;
    private readonly RenderEngineFrameInfo _renderEngineInfo;

    private readonly EngineTimeHub _timeHub;

    private readonly DebugInterfaceGateway _debugGateway;

    private bool _isDisposed = false;

    private LinearStateMachine<EngineStateLevel> _stateMachine;

    internal GameEngine(
        EngineWindow engineWindow,
        GfxRuntimeBundle<GL> gfxBundle,
        IEngineInputSource input,
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
        _engineRenderSystem = new RenderingSystem.EngineRenderSystem(engineWindow, _graphics, _assets,_eventBus);
        _coreSystems = new EngineCoreSystem(_engineRenderSystem, _inputSystem, _assets);


        _stateMachine = new LinearStateMachine<EngineStateLevel>(Enum.GetValues<EngineStateLevel>());

        var internalInput = ((EngineInputSource)input).InputContext;
        _debugGateway =
            new DebugInterfaceGateway(gfxBundle.Config.DriverContext, engineWindow.PlatformWindow, internalInput);

        _updateInfo = new UpdateFrameInfo();
        _renderEngineInfo = new RenderEngineFrameInfo(_window.OutputSize);
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
        _debugGateway.SetupCommandCallbacks(_assets);

        GfxDebugMetrics.LogEnabled = true;
    }

    private void RegisterRenderer()
    {
        var builder = _engineRenderSystem.Initialize((gfx, batchers) =>
        {
            batchers.Register(new TerrainBatcher(gfx));
        });

        _engineRenderSystem.SetupRenderer(builder);
    }

    internal void Render(float dt)
    {
        var alpha = _timeHub.Alpha;

        var frameStatus = _renderEngineInfo.BeginRenderFrame(dt, alpha, _window, _inputSystem.InputSource,
            out var frameInfo, out var runtimeParams);

        _timeHub.RenderFrame(dt);

        _debugGateway.Update(dt);

        if (_sceneManager.Current is not { } scene)
        {
            _engineRenderSystem.RenderEmptyFrame(in frameInfo);
            return;
        }

        if (_renderEngineInfo.FrameIndex > 1 && frameStatus == RenderEngineFrameInfo.BeginFrameStatus.Resize)
        {
            _timeHub.DebounceTicker ??= new DebounceTicker(30);
        }

        var beginStatus = BeginFrameStatus.None;
        if (_timeHub.DebounceTicker?.Tick() ?? false)
        {
            _timeHub.DebounceTicker = null;
            beginStatus = BeginFrameStatus.Resize;
        }

        //Todo move out
        _assets.UpdatePendingQueue(frameInfo.FrameIndex);
        while (_assets.TryProcessPendingQueue(out var req))
        {
            if (req.ResourceKind == ResourceKind.FrameBuffer)
            {
                _graphics.Gfx.Commands.BindFramebuffer(default);
                _graphics.Gfx.Commands.UnbindAllTextures();
                if(req.SpecialAction == RecreateSpecialAction.RecreateScreenDependentFbo)
                    _engineRenderSystem.RenderEngine.RecreateScreenRelativeFbo(_window.OutputSize);
                if (req.SpecialAction == RecreateSpecialAction.RecreateShadowFbo)
                {
                    var fbo = _engineRenderSystem.RenderEngine.GetRenderFbo<ShadowPassTag>(FboVariant.Default);
                    _sceneManager.Current?.InternalWorld.RenderProps.SetShadowDefault(req.Param0);
                    _engineRenderSystem.RenderEngine.RecreateFixedFrameBuffer(fbo.FboId, new Size2D(req.Param0,req.Param0));
                }
                    

            }
        }

        scene.BeforeRender(out var viewSnapshot);

        _engineRenderSystem.PreRender(beginStatus, in frameInfo, in runtimeParams, in viewSnapshot);
        _engineRenderSystem.ExecuteFrame(out var gfxFrameResult);
        _renderEngineInfo.EndRenderFrame(gfxFrameResult);

        _debugGateway.Render();


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
        _inputSystem.Update(!_debugGateway.BlockInput());
        _sceneManager.Current?.UpdateTick(tick);
    }

    private void DebugTickUpdate(int tick)
    {
        if (!_debugGateway.Enabled) return;
        _debugGateway.RefreshData(
            _assets.InternalStore,
            in _renderEngineInfo.GetRenderFrameInfo(),
            _renderEngineInfo.GfxResult);
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

        _sceneManager.ApplyPendingScene(sceneContext, builder, _engineRenderSystem, AfterBuild);

        _modules.Load(new GameModuleContext(sceneContext));
        return;

        void AfterBuild(SceneManager.SceneBuildResult result, RenderingSystem.EngineRenderSystem renderer)
        {
            renderer.AttachWorld((World)result.Context.World);
            _debugGateway.SetupBindings(_assets.Materials, (World)result.Context.World);
            foreach (var module in result.Modules) result.Context.Modules.AddModule(module());
        }
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