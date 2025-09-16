#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Core.Utils;

internal record GfxRuntimeBundle<T>(GraphicsRuntime Graphics,IGfxStartupConfig<T> Config) where T : class;
public sealed class GameEngineBuilder
{
    private AssetManagerConfiguration? _assetPipelineConfiguration = null;
    private readonly List<Func<GameScene>> _sceneFactories = new();


    internal GameEngine Build(IEngineWindowHost windowHost, IEngineInputSource input, GfxRuntimeBundle<GL> gfxBundle)
    {
        if (_assetPipelineConfiguration is null) throw new InvalidOperationException("AssetManager not configured");
        if (_sceneFactories.Count < 0) throw new InvalidOperationException("No GameScene registered");

        return new GameEngine(
            windowHost: windowHost,
            gfxBundle: gfxBundle,
            input: input,
            assetConfig: _assetPipelineConfiguration,
            sceneFactories: _sceneFactories
        );
    }

    public GameEngineBuilder RegisterScene<T>() where T : GameScene, new()
    {
        _sceneFactories.Add(static () => new T());
        return this;
    }

    public GameEngineBuilder ConfigureAssetManager(AssetManagerConfiguration assetPipelineConfiguration)
    {
        _assetPipelineConfiguration = assetPipelineConfiguration;
        return this;
    }
}