#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Patterns;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;
using Shader = ConcreteEngine.Core.Resources.Shader;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngine : IDisposable
{
    private readonly IEngineWindowHost _window;
    private readonly GraphicsRuntime _graphics;


    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;
    private readonly RenderSystem _renderer;

    private readonly ModuleManager _modules;
    private readonly FeatureManager _features;
    private readonly SceneManager _sceneManager;

    private readonly GameTime _gameTime;
    private readonly RenderTime _renderTime;

    private readonly UpdateFrameInfo _updateInfo = new();
    private readonly RenderFrameInfo _renderInfo = new();

    private bool _isDisposed = false;

    private LinearStateMachine<EngineState> _stateMachine;

    private DebounceTicker? _debounceTicker = null;

    internal GameEngine(
        IEngineWindowHost windowHost,
        GfxRuntimeBundle<GL> gfxBundle,
        IEngineInputSource input,
        AssetManagerConfiguration assetConfig,
        List<Func<GameScene>> sceneFactories
    )
    {
        _window = windowHost;
        _graphics = gfxBundle.Graphics;

        _graphics.Initialize(gfxBundle.Config);

        _sceneManager = new SceneManager(sceneFactories);

        _modules = new ModuleManager();
        _features = new FeatureManager();

        // time
        _gameTime = new GameTime(GameTickUpdate, FpsTickUpdate);
        _renderTime = new RenderTime(OnRenderTickEffect, OnGpuTickUpload, OnGpuTickDispose);

        // input
        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem(assetConfig.AssetPath, assetConfig.ManifestFilename);
        _renderer = new RenderSystem(_graphics, _window.FramebufferSize);
        _coreSystems = new EngineCoreSystem(_renderer, _inputSystem, _assets);

        _stateMachine = new LinearStateMachine<EngineState>(Enum.GetValues<EngineState>());
    }


    private void StartAssetLoader()
    {
        _assets.StartLoader(_graphics.Gfx);
    }

    private void InitializeSystems()
    {
        var materialStore = _assets.FinishLoading();
        _renderer.Initialize(materialStore, _assets);
        _coreSystems.Initialize();
    }

    internal void Render(float dt)
    {
        var fps = dt > 0 ? 1.0f / dt : 0.0f;
        _renderInfo.BeginFrame(fps, dt, _gameTime.Alpha, _window.Size, _window.FramebufferSize);
        _renderTime.Accumulate(dt);
        _renderTime.Advance();


        if (_renderInfo.FrameIndex > 1 && _renderInfo.PrevOutputSize != _renderInfo.OutputSize)
        {
            _debounceTicker ??= new DebounceTicker(30);
        }


        if (_debounceTicker?.Tick() ?? false)
        {
            _debounceTicker = null;
            var fbos = _renderer.RenderRegistry.RenderFbos;
            Span<(FrameBufferId, Size2D)> newSizes = stackalloc (FrameBufferId, Size2D)[fbos.Count];
            for (int i = 0; i < fbos.Count; i++)
                newSizes[i] = (fbos[i].FboId, fbos[i].CalculateNewSize(_renderInfo.OutputSize));

            _graphics.RecreateFbo(newSizes);
            return;
        }

        var frameInfo = _renderInfo.Frame;
        _graphics.BeginFrame(in frameInfo);
        if (_sceneManager.Current != null)
        {
            _renderTime.TickOrRenderEffect();
            _renderer.Render(_renderInfo.Alpha, in frameInfo);
        }

        _renderTime.TickOrGpuDispose();
        _renderTime.TickOrGpuUpload();
        _graphics.EndFrame(out var gfxFrameResult);
        
        _renderInfo.EndFrame(gfxFrameResult);
    }

    private void OnGpuTickDispose(int tick)
    {
    }

    private void OnGpuTickUpload(int tick)
    {
    }

    private void OnRenderTickEffect(int tick)
    {
    }

    internal void Update(float dt)
    {
        float fps = dt > 0 ? 1.0f / dt : 0.0f;
        _updateInfo.BeginFrame(fps, dt, _window.Size, _window.FramebufferSize);
        if (_stateMachine.Current != EngineState.Running)
        {
            RunSetupStateMachine();
            return;
        }

        var updateInfo =  _updateInfo.UpdateInfo;
        _sceneManager.Current?.Update(in updateInfo);

        // fixed-step simulation
        _gameTime.Advance(dt);

        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _updateInfo.GameTick = tick;
        _renderer.BeginTick(_updateInfo.UpdateInfo);
        _inputSystem.Update();
        _sceneManager.Current?.UpdateTick(tick);
        _renderer.EndTick();
    }

    private void FpsTickUpdate(int tick)
    {
        var updateInfo =  _updateInfo.UpdateInfo;
        var gfxResult = _renderInfo.GfxResult;

        Console.WriteLine(
            $"Fps: {updateInfo.Fps}; Draw Calls: {gfxResult.DrawCalls}; Triangle Count: {gfxResult.TriangleCount}");
    }


    private void RunSetupStateMachine()
    {
        switch (_stateMachine.Current)
        {
            case EngineState.NotStarted:
                _stateMachine.Next();
                break;
            case EngineState.LoadingGraphics:
                StartAssetLoader();
                _stateMachine.Next();
                break;
            case EngineState.LoadingAssets:
                _stateMachine.Next(_assets.ProcessLoader(8));
                break;
            case EngineState.InitializeSystem:
                var shaders = _assets.GetAll<Shader>();
                _renderer.InitializeGraphics(shaders);
                InitializeSystems();
                _stateMachine.Next();
                break;
            case EngineState.LoadScenes:
                _sceneManager.QueueSwitch(0);
                _stateMachine.Next();
                break;
        }
    }

    private void UpdateSceneTransitionIfNeeded()
    {
        if(!_sceneManager.HasPendingSwitch)
            return;
        
        var sceneContext = new GameSceneContext(_coreSystems) { Features = _features, Modules = _modules};
        var builder = new GameSceneConfigBuilder(_features, _modules);

        _sceneManager.ApplyPendingSwitch(sceneContext, builder, AfterBuild);
        
        _features.Load(new GameFeatureContext(sceneContext));
        _modules.Load(new GameModuleContext(sceneContext));
        return;

        void AfterBuild(SceneManager.SceneBuildResult result)
        {
            _renderer.RegisterScene(builder.RenderType, builder.RenderTargetsDesc);
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