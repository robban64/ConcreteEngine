#region

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
    private readonly InputSystem _inputSystem;
    private readonly AssetSystem _assets;
    private readonly RenderSystem _renderer;
    private readonly GameMessagePipeline _pipeline;

    private int? _nextSceneIndex = null;
    private GameScene _currentScene = null!;

    private bool _isDisposed = false;

    private float _fps;
    private FrameRenderResult _frameResult = default;
    
    private UpdateMetaInfo  _updateMeta = default;

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

        _inputSystem = new InputSystem(_input);

        // assets
        _assets = new AssetSystem(_graphics, assetConfig.AssetPath, assetConfig.ManifestFilename);
        _assets.Initialize();

        // messages
        _pipeline = new GameMessagePipeline();

        // renderer
        _renderer = new RenderSystem(_graphics, _assets.MaterialStore);
        _renderer.Initialize(_features);

        _systems = new EngineSystemManagerManager(_renderer, _inputSystem, _assets);
        _systems.Initialize();

        _nextSceneIndex = 0;
    }

    public T GetFeature<T>() where T : IGameFeature => _features.Get<T>();


    internal void Update(double delta)
    {
        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;
        _fps = fps;

        var frameCtx = new FrameMetaInfo
        {
            DeltaTime = dt,
            Fps = fps,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        _updateMeta.DeltaTime = dt;
        _updateMeta.Fps = fps;

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

    internal void Render(double delta)
    {
        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;

        var frameCtx = new FrameMetaInfo
        {
            DeltaTime = dt,
            Fps = fps,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        var snapshot = _currentScene.RenderGlobals.Snapshot;
        _renderer.Render(_updateMeta.Alpha, in frameCtx, in snapshot,  out _frameResult);
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
        if (!_nextSceneIndex.HasValue) return;
        var index = _nextSceneIndex.Value;
        if (index >= _sceneFactories.Count)
            throw new IndexOutOfRangeException($"Switch scene, index {index} is out of range.");

        var previous = _currentScene;
        previous?.Unload();

        var sceneContext = new GameSceneContext(_systems)
        {
            Features = _features,
            Modules = _modules,
        };


        var newScene = _sceneFactories[index]();
        newScene.AttachContext(sceneContext);

        var builder = new GameSceneConfigBuilder(_features, _modules);
        newScene.Build(builder);
        
        _renderer.RegisterScene(builder.RenderType, builder.RenderTargetsDesc);

        _features.Load(new GameFeatureContext(sceneContext));

        // Modules
        foreach (var (order, factory) in builder.Modules)
            _modules.AddModule(order, factory());

        _modules.Load(new GameModuleContext(sceneContext));

        // Prepare scene
        newScene.InitializeInternal();

        _currentScene = newScene;
        _nextSceneIndex = null;
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