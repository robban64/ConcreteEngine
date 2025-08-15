#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Input;
using ConcreteEngine.Core.Pipeline;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Time;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader = ConcreteEngine.Core.Assets.Shader;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngine: IDisposable
{
    private const float GameDt = 1f / 30f; // 30 Hz

    private readonly IWindow _window;

    private readonly List<Func<GameScene>> _sceneFactories;
    private readonly TypeRegistryCollection<IGameEngineSystem> _systems = new(4);
    private readonly FeatureRegistry _features = new();
    
    private readonly GameTime _gameTime;

    private IGraphicsDevice _graphics  = null!;
    
    private InputSystem _input = null!;
    private AssetSystem _assets = null!;
    private RenderSystem _renderer = null!;
    private GameMessagePipelineSystem _pipeline  = null!;
    private CameraSystem _camera = null!;
    
    private int? _nextSceneIndex = null;
    private GameScene _currentScene = null!;
    
    private bool _isDisposed = false;

    private float _fps;
    
    internal GameEngine(
        WindowOptions windowOptions,
        GraphicsBackend backend,
        AssetManagerConfiguration assetPipelineConfiguration,
        List<Func<GameScene>> sceneFactories
    )
    {
        _sceneFactories = sceneFactories;
        _gameTime = new GameTime(GameTickUpdate, FpsTickUpdate);
        
        _window = Window.Create(windowOptions);

        _window.Load += () => Load(backend, assetPipelineConfiguration);
        _window.Update += Update;
        _window.Render += Render;
        _window.Closing += Close;
    }

    public void Run()
    {
        _window.Run();
        _window?.Dispose();
    }
    
    public T GetFeature<T>() where T : IGameFeature => _features.Get<T>();
    public void RegisterFeature<T>() where T : IGameFeature, new()
        => _features.RegisterFeature<T>();

    public T GetSystem<T>() where T: IGameEngineSystem =>  (T)_systems.Get<T>();

    private void GameTickUpdate(int tick)
    {
        _features.GameTickUpdate(tick);
    }

    private void FpsTickUpdate(int tick)
    {
        Console.WriteLine($"Fps: {_fps} with tick {tick}");
    }

    private void Load(GraphicsBackend backend, AssetManagerConfiguration assetPipelineConfiguration)
    {

        LoadGraphics(backend);
        LoadSystems(assetPipelineConfiguration);
        
        _nextSceneIndex = 0;
    }

    private void LoadGraphics(GraphicsBackend backend)
    {
        var initialFrameContext = new GraphicsFrameContext
        {
            DeltaTime = 0,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        _graphics = backend switch
        {
            GraphicsBackend.OpenGL => new GlGraphicsDevice(_window.CreateOpenGL(), in initialFrameContext),
            _ => throw new GraphicsException("Invalid GraphicsBackend. Only OpenGL supported")
        };
    }
    private void LoadSystems(AssetManagerConfiguration assetPipelineConfiguration)
    {
        // input
        _input = new InputSystem(_window.CreateInput());
        
        // camera
        _camera = new CameraSystem(_input);

        // assets
        _assets = new AssetSystem(graphics: _graphics, assetPath: assetPipelineConfiguration.AssetPath,
            manifestFilename: assetPipelineConfiguration.ManifestFilename);
        _assets.LoadFromManifest();

        // messages
        _pipeline = new GameMessagePipelineSystem();
        
        // renderer
        var shaders = _assets.GetAll<Shader>();
        _renderer = new RenderSystem(_graphics, _camera.Transform, shaders.ToArray());

        _systems.Register<InputSystem>(_input);
        _systems.Register<CameraSystem>(_camera);
        _systems.Register<GameMessagePipelineSystem>(_pipeline);
        _systems.Register<AssetSystem>(_assets);
        _systems.Register<RenderSystem>(_renderer);

    }

    private void Update(double delta)
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
        
        _input.Update();
        _camera.Update(in frameCtx);
        
        // fixed-step simulation
        _gameTime.Advance(dt);

        // TODO: Store for render use
        // Usage: Vector2.Lerp(prev.Pos, curr.Pos, renderAlpha);
        //float renderAlpha = _gameTimer.Accumulator / GameDt;
        
        UpdateSceneTransitionIfNeeded();
    }

    private void Render(double delta)
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

        _graphics.StartFrame(in frameCtx);
        _renderer.Prepare();
        _graphics.StartDraw();
        _renderer.Execute(_gameTime.Alpha);
        _graphics.EndFrame();
    }


    private void Close()
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
        if(index >= _sceneFactories.Count)
            throw new IndexOutOfRangeException($"Switch scene, index {index} is out of range.");
        
        var previous = _currentScene;
        previous?.Unload();

        var sceneContext = new GameSceneContext(this);
        
        var newScene = _sceneFactories[index]();
        newScene.AttachContext(sceneContext);
        newScene.Configure();
        _features.Load(new GameFeatureContext(sceneContext));
        newScene.OnReady();
        
        _currentScene = newScene;
        _nextSceneIndex = null;
    }

    
    public void Dispose()
    {
        if(_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;

        _assets?.Dispose();
        _graphics?.Dispose();
    }
    
}