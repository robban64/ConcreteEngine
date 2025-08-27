#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Utils;

public sealed class GameEngineBuilder
{
    private AssetManagerConfiguration? _assetPipelineConfiguration = null;
    private readonly List<Func<GameScene>> _sceneFactories = new();


    public GameEngine Build(IEngineWindowHost windowHost, IEngineInputSource input, IGraphicsDevice graphics)
    {
        if (_assetPipelineConfiguration is null) throw new InvalidOperationException("AssetManager not configured");
        if (_sceneFactories.Count < 0) throw new InvalidOperationException("No GameScene registered");

        return new GameEngine(
            windowHost: windowHost,
            graphics: graphics,
            input: input,
            assetConfig: _assetPipelineConfiguration,
            sceneFactories: _sceneFactories
        );
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