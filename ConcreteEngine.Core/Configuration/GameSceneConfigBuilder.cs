#region

using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

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


public sealed class GameSceneConfigBuilder(FeatureManager features, ModuleManager modules)
    : IGameSceneRenderBuilder, IGameSceneModuleBuilder
{
    private readonly SortedList<int, Func<GameModule>> _modules = new();
    private RenderTargetDescriptor _renderTargetsDesc = null!;

    
    internal void Clear()
    {
        _modules.Clear();
    }
    
    public RenderType  RenderType { get; private set; }

    public RenderTargetDescriptor RenderTargetsDesc => _renderTargetsDesc;
    public SortedList<int, Func<GameModule>> Modules => _modules;


    public void RegisterRender2D(RenderTargetDescriptor desc)
    {
        _renderTargetsDesc = desc;
        RenderType  = RenderType.Render2D;
    }
    
    public void RegisterRender3D(RenderTargetDescriptor desc)
    {
        _renderTargetsDesc = desc;
        RenderType = RenderType.Render3D;
    }

    public void RegisterModule<T>(int order) where T : GameModule, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _modules.Add(order, () => new T());
    }
}