#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

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
    //private readonly GameMessagePipeline _pipeline;
    
    private readonly List<Func<GameScene>> _sceneFactories;
    private readonly ModuleManager _modules;
    private readonly FeatureManager _features;

    private readonly GameTime _gameTime;
    private readonly RenderTime _renderTime;


    private int _nextSceneIndex = -1;
    private GameScene _currentScene = null!;

    private bool _isDisposed = false;

    private float _fps;
    private long _frameIdx = -1;

    private UpdateInfo _updateCtx;
    private FrameInfo _frameCtx;
    private GpuFrameStats _gpuFrameResult;

    private Vector2D<int> _prevOutputSize;

    private LinearStateMachine<EngineState> _stateMachine;

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
        _sceneFactories = sceneFactories;
        
        _graphics.Initialize(gfxBundle.Config);

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
        _renderer.Initialize(materialStore);
        _systems.Initialize();
    }

    internal void Render(double delta)
    {
        var dt = (float)delta;
        var outputSize = _window.FramebufferSize;
        var frameCtx = new FrameInfo(
            frameIndex: _frameIdx,
            deltaTime:dt,
            vSyncEnabled: false,
            resizePending: _frameIdx > 1 && outputSize != _prevOutputSize,
            viewport: _window.Size,
            outputSize: outputSize
        );
        
        _renderTime.Accumulate(dt);
        _renderTime.Advance();
        
        _graphics.BeginFrame(in frameCtx);
        if (_currentScene != null)
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

        _currentScene?.Update(in _updateCtx);

        // fixed-step simulation
        _gameTime.Advance(dt);

        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _updateCtx.GameTick = tick;
        _renderer.BeginTick(in _updateCtx);
        _input.Update();
        _currentScene?.UpdateTick(tick);
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
                _renderer.InitializeGraphics();
                StartAssetLoader();
                _stateMachine.Next();
                break;
            case EngineState.LoadingAssets:
                _stateMachine.Next(_assets.ProcessLoader(8));
                break;
            case EngineState.InitializeSystem:
                InitializeSystems();
                _stateMachine.Next();
                break;
            case EngineState.LoadScenes:
                _nextSceneIndex = 0;
                _stateMachine.Next();
                break;
        }
    }
    
    private void UpdateSceneTransitionIfNeeded()
    {
        if (_nextSceneIndex < 0) return;
        var index = _nextSceneIndex;
        if (index >= _sceneFactories.Count)
            throw new IndexOutOfRangeException($"Switch scene, index {index} is out of range.");

        var previous = _currentScene;
        previous?.Unload();

        var sceneContext = new GameSceneContext(_systems) { Features = _features, Modules = _modules, };


        var newScene = _sceneFactories[index]();
        newScene.AttachContext(sceneContext);

        var builder = new GameSceneConfigBuilder(_features, _modules);
        newScene.Build(builder);

        _renderer.RegisterScene(_window.FramebufferSize, builder.RenderType, builder.RenderTargetsDesc);

        _features.Load(new GameFeatureContext(sceneContext));

        // Modules
        foreach (var factory in builder.Modules)
            _modules.AddModule(factory());

        _modules.Load(new GameModuleContext(sceneContext));

        // Prepare scene
        newScene.InitializeInternal();

        _currentScene = newScene;
        _nextSceneIndex = -1;
        builder.Clear();
    }

    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _currentScene?.Unload();
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