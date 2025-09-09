#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Messaging;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngine : IDisposable
{
    private const float GameDt = 1f / 10f; // 30 Hz

    private readonly IEngineWindowHost _window;

    private readonly List<Func<GameScene>> _sceneFactories;
    private readonly ModuleManager _modules;
    private readonly FeatureManager _features;

    private readonly GameTime _gameTime;

    private readonly IGraphicsDevice _graphics;

    private readonly IEngineInputSource _input;

    private readonly EngineSystemManagerManager _systems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;
    private readonly RenderSystem _renderer;
    //private readonly GameMessagePipeline _pipeline;

    private int _nextSceneIndex = -1;
    private GameScene _currentScene = null!;

    private bool _isDisposed = false;

    private float _fps;
    private FrameRenderResult _frameResult = default;
    private UpdateMetaInfo _updateMeta = default;


    private enum EngineState
    {
        NotStarted,
        LoadingGraphics,
        LoadingAssets,
        InitializeSystem,
        LoadScenes,
        Running
    }

    private LinearStateMachine<EngineState> _stateMachine;

    internal GameEngine(
        IEngineWindowHost windowHost,
        IGraphicsDevice graphics,
        IEngineInputSource input,
        AssetManagerConfiguration assetConfig,
        List<Func<GameScene>> sceneFactories
    )
    {
        _window = windowHost;
        _graphics = graphics;
        _input = input;
        _sceneFactories = sceneFactories;

        _modules = new ModuleManager();
        _features = new FeatureManager();

        // time
        _gameTime = new GameTime(GameTickUpdate, FpsTickUpdate);

        // input
        _inputSystem = new InputSystem(_input);

        // assets
        _assets = new AssetSystem(assetConfig.AssetPath, assetConfig.ManifestFilename);

        // messages
        //_pipeline = new GameMessagePipeline();

        // renderer
        _renderer = new RenderSystem(_graphics);

        _systems = new EngineSystemManagerManager(_renderer, _inputSystem, _assets);

        _stateMachine = new LinearStateMachine<EngineState>(Enum.GetValues<EngineState>());
    }

    private void StartAssetLoader()
    {
        var uploadSink = _graphics.CreateUploader();
        _assets.StartLoader(uploadSink);
    }

    private void InitializeSystems()
    {
        var materialStore = _assets.FinishLoading();
        _renderer.Initialize(materialStore);
        _systems.Initialize();
    }

    internal void Render(double delta)
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
                _stateMachine.Next(_assets.ProcessLoader(4));
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

        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;

        var frameCtx = new FrameMetaInfo
        {
            DeltaTime = dt, Fps = fps, FramebufferSize = _window.FramebufferSize, ViewportSize = _window.Size
        };

        if (_currentScene != null)
        {
            var snapshot = _currentScene.RenderGlobals.Snapshot;
            _renderer.Render(_updateMeta.Alpha, in frameCtx, in snapshot, out _frameResult);
        }
        else
        {
            _renderer.RenderBlank(in frameCtx, out _frameResult);
        }
    }

    internal void Update(double delta)
    {
        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;
        _fps = fps;

        var frameCtx = new FrameMetaInfo
        {
            DeltaTime = dt, Fps = fps, FramebufferSize = _window.FramebufferSize, ViewportSize = _window.Size
        };

        _updateMeta.DeltaTime = dt;
        _updateMeta.Fps = fps;


        if (_stateMachine.Current != EngineState.Running) return;

        _currentScene?.Update(in frameCtx);

        // fixed-step simulation
        _gameTime.Advance(dt);

        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _updateMeta.GameTick = tick;
        _renderer.BeginTick(in _updateMeta);
        _input.Update();
        _currentScene?.UpdateTick(tick);
        _renderer.EndTick();
    }

    private void FpsTickUpdate(int tick)
    {
        Console.WriteLine($"Tick {tick}");
        Console.WriteLine(
            $"Fps: {_fps}; Draw Calls: {_frameResult.DrawCalls}; Triangle Count: {_frameResult.TriangleCount}");
    }


    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _currentScene?.Unload();
        _assets?.Shutdown();
        _graphics?.Dispose();
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

        _renderer.RegisterScene(builder.RenderType, builder.RenderTargetsDesc);

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


    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;

        _assets?.Shutdown();
        _graphics?.Dispose();
    }
}