using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Rendering.Renderers;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Configuration;

public interface IGameSceneRenderBuilder
{
    void RegisterRenderPass(RenderTargetId target, int order, IRenderPass pass);

    void RegisterEmitter<TEmitter, TEntity>(int order)
        where TEmitter : DrawCommandEmitter<TEntity>, new()
        where TEntity : struct;


    public void RegisterRenderer<TCommand, TRenderer>(DrawCommandId commandId, DrawCommandTag commandTag)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>;
}

public interface IGameSceneFeatureBuilder
{
    void RegisterDrawFeature<TEmitter, TFeature, TEntity>(int order)
        where TFeature : class, IGameFeature, IDrawableFeature<TEntity>, new()
        where TEmitter : DrawCommandEmitter<TEntity>
        where TEntity : struct;

    void RegisterFeature<T>(int order) where T : IGameFeature, new();
}

public sealed class GameSceneConfigBuilder()
    : IGameSceneRenderBuilder, IGameSceneFeatureBuilder
{
    private readonly SortedList<int, Func<IGameFeature>> _features = new(8);
    private readonly SortedList<int, (Func<IDrawableFeature>, Type)> _drawFeatures = new(8);

    private readonly SortedList<int, Func<IDrawCommandEmitter>> _emitters = new(8);
    private readonly SortedList<int, RenderPassRegistryMeta> _passes = new(8);
    private readonly List<RendererRegistry> _renderers = new(8);

    internal void Clear()
    {
        _features.Clear();
        _emitters.Clear();
        _passes.Clear();
        _renderers.Clear();
    }

    public SortedList<int, Func<IGameFeature>> Features => _features;
    public SortedList<int, (Func<IDrawableFeature>, Type)> DrawFeatures => _drawFeatures;
    public SortedList<int, Func<IDrawCommandEmitter>> Emitters => _emitters;
    public SortedList<int, RenderPassRegistryMeta> Passes => _passes;
    public List<RendererRegistry> Renderers => _renderers;


    public void RegisterFeature<T>(int order) where T : IGameFeature, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _features.Add(order, () => new T());
    }

    public void RegisterDrawFeature<TEmitter, TFeature, TEntity>(int order)
        where TFeature : class, IGameFeature, IDrawableFeature<TEntity>, new()
        where TEmitter : DrawCommandEmitter<TEntity>
        where TEntity : struct
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));

        _drawFeatures.Add(order, (() => new TFeature(), typeof(TEmitter)));
    }

    public void RegisterEmitter<TEmitter, TEntity>(int order)
        where TEmitter : DrawCommandEmitter<TEntity>, new()
        where TEntity : struct
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _emitters.Add(order, () => new TEmitter());
    }

    public void RegisterRenderPass(RenderTargetId target, int order, IRenderPass pass)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _passes.Add(order, new RenderPassRegistryMeta( target, pass));
    }

    public void RegisterRenderer<TCommand,TRenderer>(DrawCommandId commandId, DrawCommandTag commandTag)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>
    {
        var registry = new RendererRegistry(commandId, commandTag,
            (submitter, cmdId, tag) => submitter.Register<TCommand, TRenderer>(cmdId,tag));
        //_receiverBindings.Add(new ReceiverRegistry(reqId, (r, reqId) => r.Register<T>(reqId)));
        _renderers.Add(registry);
    }

    public record struct RenderPassRegistryMeta(RenderTargetId Target, IRenderPass Pass);

    public record struct RendererRegistry(
        DrawCommandId CommandId,
        DrawCommandTag CommandTag,
        Action<DrawCommandSubmitter, DrawCommandId, DrawCommandTag> Bind);
}