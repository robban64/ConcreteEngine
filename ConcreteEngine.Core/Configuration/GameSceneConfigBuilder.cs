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
    void RegisterRenderTargets(RenderTargetDescription desc);
}


public sealed class GameSceneConfigBuilder(FeatureManager features, ModuleManager modules)
    : IGameSceneRenderBuilder, IGameSceneModuleBuilder
{
    private readonly SortedList<int, Func<GameModule>> _modules = new();
    private RenderTargetDescription _renderTargetsDesc = null!;

    
    internal void Clear()
    {
        _modules.Clear();
    }

    public RenderTargetDescription RenderTargetsDesc => _renderTargetsDesc;
    public SortedList<int, Func<GameModule>> Modules => _modules;


    public void RegisterRenderTargets(RenderTargetDescription desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.SceneTarget);
        ArgumentNullException.ThrowIfNull(desc.LightTarget);
        ArgumentNullException.ThrowIfNull(desc.ScreenTarget);
        desc.LightTarget.LightShader.IsValidOrThrow();
        desc.ScreenTarget.CompositeShaderId.IsValidOrThrow();
        
        _renderTargetsDesc = desc;
    }

    public void RegisterModule<T>(int order) where T : GameModule, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _modules.Add(order, () => new T());
    }
}