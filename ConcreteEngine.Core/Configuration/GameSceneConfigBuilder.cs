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
    
    void RegisterDrawProducer<TProducer, TEntity>(int order)
        where TProducer : DrawCommandProducer<TEntity>, new()
        where TEntity : class;

    public void RegisterRenderer<TCommand, TRenderer>(DrawCommandTag commandTag, params DrawCommandId[] commandIds)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>;
}

public interface IGameSceneFeatureBuilder
{
    void RegisterDrawFeature<TProducer, TFeature, TDrawData>(int order)
        where TFeature : class, IGameFeature, IDrawableFeature<TDrawData>, new()
        where TProducer : DrawCommandProducer<TDrawData>
        where TDrawData : class;

    void RegisterFeature<T>(int order) where T : IGameFeature, new();
}

public sealed class GameSceneConfigBuilder(IGraphicsDevice graphics, FeatureManager features, ModuleManager modules)
    : IGameSceneRenderBuilder, IGameSceneFeatureBuilder, IGameSceneModuleBuilder
{
    private readonly SortedList<int, Func<IGameFeature>> _features = new();
    private readonly SortedList<int, (Func<IDrawableFeature>, Type)> _drawFeatures = new();
    private readonly SortedList<int, Func<IDrawCommandProducer>> _drawProducers = new();
    private readonly SortedList<int, Func<GameModule>> _modules = new();
    private readonly List<RendererRegistry> _renderers = new();

    private RenderTargetDescription _renderTargetsDesc = null!;

    internal void Clear()
    {
        _features.Clear();
        _drawProducers.Clear();
        _renderers.Clear();
    }

    public IGraphicsDevice GraphicsDevice { get; } = graphics;

    public FeatureManager FeatureManager { get; } = features;
    public ModuleManager ModuleManager { get; } = modules;

    public SortedList<int, Func<IGameFeature>> Features => _features;
    public SortedList<int, (Func<IDrawableFeature>, Type)> DrawFeatures => _drawFeatures;
    public SortedList<int, Func<IDrawCommandProducer>> DrawProducers => _drawProducers;
    public RenderTargetDescription RenderTargetsDesc => _renderTargetsDesc;
    public SortedList<int, Func<GameModule>> Modules => _modules;

    public List<RendererRegistry> Renderers => _renderers;


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
    public void RegisterFeature<T>(int order) where T : IGameFeature, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _features.Add(order, () => new T());
    }

    public void RegisterDrawFeature<TProducer, TFeature, TDrawData>(int order)
        where TFeature : class, IGameFeature, IDrawableFeature<TDrawData>, new()
        where TProducer : DrawCommandProducer<TDrawData>
        where TDrawData : class
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));

        _drawFeatures.Add(order, (() => new TFeature(), typeof(TProducer)));
    }

    public void RegisterDrawProducer<TProducer, TDrawData>(int order)
        where TProducer : DrawCommandProducer<TDrawData>, new()
        where TDrawData : class
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _drawProducers.Add(order, () => new TProducer());
    }

    public void RegisterRenderer<TCommand, TRenderer>(DrawCommandTag commandTag, params DrawCommandId[] commandIds)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>
    {
        var registry = new RendererRegistry(commandIds, commandTag,
            (submitter, cmdId, tag) => submitter.Register<TCommand, TRenderer>(cmdId, tag));
        _renderers.Add(registry);
    }


    public record RendererRegistry(
        DrawCommandId[] CommandIds,
        DrawCommandTag CommandTag,
        Action<DrawCommandSubmitter, DrawCommandId, DrawCommandTag> Bind);

    public void RegisterModule<T>(int order) where T : GameModule, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _modules.Add(order, () => new T());
    }
}