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
    private GameProgram? _program = null;


    public GameEngine Build()
    {
        if (!_windowOptions.HasValue) throw new InvalidOperationException("WindowOptions not set");
        if (!_graphicsBackend.HasValue) throw new InvalidOperationException("GraphicsBackend not set");
        if (_assetPipelineConfiguration is null) throw new InvalidOperationException("AssetManager not configured");
        if (_program is null) throw new InvalidOperationException("GameProgram not configured");

        return new GameEngine(
            program:_program,
            windowOptions: _windowOptions.Value,
            backend: _graphicsBackend.Value,
            assetPipelineConfiguration: _assetPipelineConfiguration
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
    
    public GameEngineBuilder BindProgram(GameProgram program)
    {
        _program = program;
        return this;
    } 
    public GameEngineBuilder ConfigureAssetManager(AssetManagerConfiguration assetPipelineConfiguration)
    {
        _assetPipelineConfiguration = assetPipelineConfiguration;
        return this;
    }
    

}