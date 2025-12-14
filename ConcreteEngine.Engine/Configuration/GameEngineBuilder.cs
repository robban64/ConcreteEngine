using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Graphics;
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

    internal GameEngine Build(EngineWindow engineWindow, EngineInputSource input, GfxRuntimeBundle<GL> gfxBundle)
    {
        if (_sceneFactories.Count < 0) throw new InvalidOperationException("No GameScene registered");

        return new GameEngine(
            engineWindow: engineWindow,
            gfxBundle: gfxBundle,
            input: input,
            sceneFactories: _sceneFactories
        );
    }

    public GameEngineBuilder RegisterScene<T>() where T : GameScene, new()
    {
        _sceneFactories.Add(static () => new T());
        return this;
    }
}