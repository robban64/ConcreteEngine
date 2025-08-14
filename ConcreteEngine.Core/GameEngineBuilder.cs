#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngineBuilder
{
    private WindowOptions? _windowOptions = null;
    private GraphicsBackend? _graphicsBackend = null;
    private AssetManagerConfiguration? _assetPipelineConfiguration = null;
    private readonly List<Func<GameScene>> _sceneFactories = new();


    public GameEngine Build()
    {
        if (!_windowOptions.HasValue) throw new InvalidOperationException("WindowOptions not set");
        if (!_graphicsBackend.HasValue) throw new InvalidOperationException("GraphicsBackend not set");
        if (_assetPipelineConfiguration is null) throw new InvalidOperationException("AssetManager not configured");
        if (_sceneFactories.Count < 0) throw new InvalidOperationException("No GameScene registered");

        return new GameEngine(
            windowOptions: _windowOptions.Value,
            backend: _graphicsBackend.Value,
            assetPipelineConfiguration: _assetPipelineConfiguration,
            sceneFactories: _sceneFactories
        );
    }


    public GameEngineBuilder WithWindowOptions(Func<WindowOptions> factory)
    {
        _windowOptions = factory.Invoke();
        return this;
    }

    public GameEngineBuilder WithGraphicsBackend(GraphicsBackend graphicsBackend)
    {
        _graphicsBackend = graphicsBackend;
        return this;
    }
    
    public GameEngineBuilder RegisterScene<T>() where T : GameScene, new()
    {
        _sceneFactories.Add(() => new T());
        return this;
    } 
    
    public GameEngineBuilder ConfigureAssetManager(AssetManagerConfiguration assetPipelineConfiguration)
    {
        _assetPipelineConfiguration = assetPipelineConfiguration;
        return this;
    }
    

}