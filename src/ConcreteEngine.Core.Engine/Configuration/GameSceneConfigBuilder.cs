using ConcreteEngine.Core.Engine.Scene.Modules;

namespace ConcreteEngine.Core.Engine.Configuration;

public interface IGameSceneModuleBuilder
{
    void RegisterModule<T>(int order) where T : GameModule, new();
}

public sealed class GameSceneConfigBuilder : IGameSceneModuleBuilder
{
    private readonly List<Func<GameModule>> _modules = [];

    public IReadOnlyList<Func<GameModule>> Modules => _modules;


    internal void Clear() => _modules.Clear();

    public void RegisterModule<T>(int order) where T : GameModule, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);
        _modules.Add(ModuleFactory<T>);
    }

    private static GameModule ModuleFactory<T>() where T : GameModule, new() => new T();
}