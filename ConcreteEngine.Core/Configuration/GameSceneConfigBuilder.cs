#region

using ConcreteEngine.Core.Scene.Modules;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Descriptors;

#endregion

namespace ConcreteEngine.Core.Configuration;

public interface IGameSceneModuleBuilder
{
    void RegisterModule<T>(int order) where T : GameModule, new();
}

public interface IGameSceneRenderBuilder
{
    void RegisterRender2D(RenderTargetDescriptor desc);
    void RegisterRender3D(RenderTargetDescriptor desc);
}

public sealed class GameSceneConfigBuilder(ModuleManager modules)
    : IGameSceneRenderBuilder, IGameSceneModuleBuilder
{
    private readonly List<Func<GameModule>> _modules = new();
    private RenderTargetDescriptor _renderTargetsDesc = null!;
    public RenderType RenderType { get; private set; }

    public RenderTargetDescriptor RenderTargetsDesc => _renderTargetsDesc;
    public IReadOnlyList<Func<GameModule>> Modules => _modules;


    internal void Clear() => _modules.Clear();

    public void RegisterRender2D(RenderTargetDescriptor desc)
    {
        _renderTargetsDesc = desc;
        RenderType = RenderType.Render2D;
    }

    public void RegisterRender3D(RenderTargetDescriptor desc)
    {
        _renderTargetsDesc = desc;
        RenderType = RenderType.Render3D;
    }

    public void RegisterModule<T>(int order) where T : GameModule, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _modules.Add(ModuleFactory<T>);
    }

    private static GameModule ModuleFactory<T>() where T : GameModule, new() => new T();
}