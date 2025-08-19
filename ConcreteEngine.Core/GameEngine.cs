#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Pipeline;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader = ConcreteEngine.Core.Resources.Shader;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngine : IDisposable
{
    private const float GameDt = 1f / 10f; // 30 Hz

    private readonly IEngineWindowHost _window;

    private readonly List<Func<GameScene>> _sceneFactories;
    private readonly TypeRegistryCollection<IGameEngineSystem> _systems = new(4);
    private readonly FeatureRegistry _features = new();

    private readonly GameTime _gameTime;

    private readonly IGraphicsDevice _graphics;

    private readonly IEngineInputSource _input;
    private readonly AssetSystem _assets;
    private readonly RenderSystem _renderer;
    private readonly GameMessagePipelineSystem _pipeline;
    private readonly CameraSystem _camera;

    private int? _nextSceneIndex = null;
    private GameScene _currentScene = null!;

    private bool _isDisposed = false;

    private float _fps;

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
        
        // time
        _gameTime = new GameTime(GameTickUpdate, FpsTickUpdate);
        
        // camera
        _camera = new CameraSystem(_input);

        // assets
        _assets = new AssetSystem(_graphics, assetConfig.AssetPath, assetConfig.ManifestFilename);
        _assets.LoadFromManifest();

        // messages
        _pipeline = new GameMessagePipelineSystem();

        // renderer
        var shaders = _assets.GetAll<Shader>();
        _renderer = new RenderSystem(_graphics, _camera.Transform, shaders.ToArray());

        _systems.Register<CameraSystem>(_camera);
        _systems.Register<GameMessagePipelineSystem>(_pipeline);
        _systems.Register<AssetSystem>(_assets);
        _systems.Register<RenderSystem>(_renderer);

        _nextSceneIndex = 0;
    }

    public T GetFeature<T>() where T : IGameFeature => _features.Get<T>();

    public T GetSystem<T>() where T : IGameEngineSystem => (T)_systems.Get<T>();

    private void GameTickUpdate(int tick)
    {
        var viewportSize = _window.Size;
        _input.Update();

        _features.GameTickUpdate(tick);
    }

    private void FpsTickUpdate(int tick)
    {
        //Console.WriteLine($"Viewport: {_window.Size}, FrameBufSize: {_window.FramebufferSize}");
        Console.WriteLine($"Fps: {_fps} with tick {tick}");
    }
    
    internal void Update(double delta)
    {
        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;
        _fps = fps;

        var frameCtx = new GraphicsFrameContext
        {
            DeltaTime = dt,
            FramesPerSecond = fps,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        _camera.Update(in frameCtx);

        // fixed-step simulation
        _gameTime.Advance(dt);

        // TODO: Store for render use
        // Usage: Vector2.Lerp(prev.Pos, curr.Pos, renderAlpha);
        //float renderAlpha = _gameTimer.Accumulator / GameDt;

        UpdateSceneTransitionIfNeeded();
    }

    internal void Render(double delta)
    {
        float dt = (float)delta;
        float fps = dt > 0 ? 1.0f / dt : 0.0f;

        var frameCtx = new GraphicsFrameContext
        {
            DeltaTime = dt,
            FramesPerSecond = fps,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        _renderer.Render(_gameTime.Alpha, in frameCtx);
    }


    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;

        _assets?.Dispose();
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

        var sceneContext = new GameSceneContext(this);

        var builder = new GameSceneConfigBuilder();
        
        
        var newScene = _sceneFactories[index]();
        newScene.AttachContext(sceneContext);
        
        newScene.ConfigureRenderer(builder);
        _renderer.Initialize(builder);
        
        newScene.ConfigureFeatures(builder);

        foreach (var (order, factory) in builder.Features)
            _features.AddFeature(order, factory());

        foreach (var (order, it) in builder.DrawFeatures)
        {
            var (factory, emitterType) = it;
            var feature = factory();
            _features.AddFeature(order, feature);
            _renderer.RegisterDrawFeature(order, feature, emitterType);
        }
        
        _features.Load(new GameFeatureContext(sceneContext));

        newScene.Initialize(_graphics);

        _currentScene = newScene;
        _nextSceneIndex = null;
    }


    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;

        _assets?.Dispose();
        _graphics?.Dispose();
    }
}