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
    private enum EngineState
    {
        NotStarted,
        LoadingGraphics,
        LoadingAssets,
        InitializeSystem,
        LoadScenes,
        Running
    }

    private readonly IEngineWindowHost _window;
    private readonly GraphicsRuntime _graphics;
    private readonly IEngineInputSource _input;


    private readonly EngineSystemManagerManager _systems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;
    private readonly RenderSystem _renderer;

    private readonly ModuleManager _modules;
    private readonly FeatureManager _features;
    private readonly SceneManager _sceneManager;

    private readonly GameTime _gameTime;
    private readonly RenderTime _renderTime;

    private readonly EngineFrameInfo _frameInfo = new();

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
        _input = input;

        _graphics.Initialize(gfxBundle.Config);

        _sceneManager = new SceneManager(sceneFactories);

        _modules = new ModuleManager();
        _features = new FeatureManager();

        // time
        _gameTime = new GameTime(GameTickUpdate, FpsTickUpdate);
        _renderTime = new RenderTime(OnRenderTickEffect, OnGpuTickUpload, OnGpuTickDispose);

        // input
        _inputSystem = new InputSystem(_input);

        // assets
        _assets = new AssetSystem(assetConfig.AssetPath, assetConfig.ManifestFilename);

        // messages
        //_pipeline = new GameMessagePipeline();

        // renderer
        _renderer = new RenderSystem(_graphics, _window.FramebufferSize);

        _systems = new EngineSystemManagerManager(_renderer, _inputSystem, _assets);

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
        _systems.Initialize();
    }

    internal void Render(double delta)
    {
        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;
        _frameInfo.BeginFrame(fps, dt, _window.Size, _window.FramebufferSize);
        _renderTime.Accumulate(dt);
        _renderTime.Advance();


        if (_frameIdx > 1 && outputSize != _prevOutputSize)
        {
            _debounceTicker ??= new DebounceTicker(30);
        }


        if (_debounceTicker?.Tick() ?? false)
        {
            _debounceTicker = null;
            var fbos = _renderer.RenderRegistry.RenderFbos;
            Span<(FrameBufferId, Size2D)> newSizes = stackalloc (FrameBufferId, Size2D)[fbos.Count];
            for (int i = 0; i < fbos.Count; i++)
                newSizes[i] = (fbos[i].FboId, fbos[i].CalculateNewSize(outputSize));

            _graphics.RecreateFbo(newSizes);
            return;
        }

        _graphics.BeginFrame(in frameCtx);
        if (_sceneManager.Current != null)
        {
            _renderTime.TickOrRenderEffect();
            _renderer.Render(_updateCtx.Alpha, in frameCtx);
        }

        _renderTime.TickOrGpuDispose();
        _renderTime.TickOrGpuUpload();
        _graphics.EndFrame(out _gpuFrameResult);
        _prevOutputSize = _window.FramebufferSize;
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

    internal void Update(double delta)
    {
        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;
        _fps = fps;
        _frameIdx++;

        _updateCtx.DeltaTime = dt;
        _updateCtx.Fps = fps;


        if (_stateMachine.Current != EngineState.Running)
        {
            RunSetupStateMachine();
            return;
        }

        _sceneManager.Current?.Update(in _updateCtx);

        // fixed-step simulation
        _gameTime.Advance(dt);

        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _updateCtx.GameTick = tick;
        _renderer.BeginTick(in _updateCtx);
        _input.Update();
        _sceneManager.Current?.UpdateTick(tick);
        _renderer.EndTick();
    }

    private void FpsTickUpdate(int tick)
    {
        Console.WriteLine(
            $"Fps: {_fps}; Draw Calls: {_gpuFrameResult.DrawCalls}; Triangle Count: {_gpuFrameResult.TriangleCount}");
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
        
        var sceneContext = new GameSceneContext(_systems) { Features = _features, Modules = _modules};
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