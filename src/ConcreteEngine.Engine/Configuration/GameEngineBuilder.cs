using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Configuration;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Engine.Configuration;

internal sealed class GfxRuntimeBundle<T>(GraphicsRuntime graphics, IGfxStartupConfig<T> config) where T : class
{
    public readonly GraphicsRuntime Graphics = graphics;
    public readonly IGfxStartupConfig<T> Config = config;
}

public sealed class GameEngineBuilder
{
    private readonly List<Func<GameScene>> _sceneFactories = [];

    internal GameEngine Build(GfxRuntimeBundle<GL> gfxBundle)
    {
        if (_sceneFactories.Count < 0) throw new InvalidOperationException("No GameScene registered");

        return new GameEngine(
            gfxBundle: gfxBundle,
            sceneFactories: _sceneFactories
        );
    }

    public GameEngineBuilder RegisterScene<T>() where T : GameScene, new()
    {
        _sceneFactories.Add(static () => new T());
        return this;
    }
}