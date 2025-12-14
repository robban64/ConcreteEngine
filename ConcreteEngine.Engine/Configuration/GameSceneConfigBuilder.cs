#region

using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Renderer.Descriptors;

#endregion

namespace ConcreteEngine.Engine.Configuration;

public interface IGameSceneModuleBuilder
{
    void RegisterModule<T>(int order) where T : GameModule, new();
}

public interface IGameSceneRenderBuilder
{
    void RegisterRender(RenderTargetDescriptor desc);
}

public sealed class GameSceneConfigBuilder(ModuleManager modules)
    : IGameSceneRenderBuilder, IGameSceneModuleBuilder
{
    private readonly List<Func<GameModule>> _modules = [];
    private RenderTargetDescriptor _renderTargetsDesc = null!;

    public RenderTargetDescriptor RenderTargetsDesc => _renderTargetsDesc;
    public IReadOnlyList<Func<GameModule>> Modules => _modules;


    internal void Clear() => _modules.Clear();


    public void RegisterRender(RenderTargetDescriptor desc)
    {
        _renderTargetsDesc = desc;
    }

    public void RegisterModule<T>(int order) where T : GameModule, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);
        _modules.Add(ModuleFactory<T>);
    }

    private static GameModule ModuleFactory<T>() where T : GameModule, new() => new T();
}