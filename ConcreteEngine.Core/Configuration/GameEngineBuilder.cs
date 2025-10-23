#region

using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Core.Configuration;

internal record GfxRuntimeBundle<T>(GraphicsRuntime Graphics, IGfxStartupConfig<T> Config) where T : class;

public sealed class GameEngineBuilder
{
    private readonly List<Func<GameScene>> _sceneFactories = new();

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