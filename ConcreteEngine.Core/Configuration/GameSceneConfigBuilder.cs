#region

using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Rendering.Renderers;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Modules;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Configuration;

public interface IGameSceneModuleBuilder
{
    void RegisterModule<T>(int order) where T : GameModule, new();
}

public interface IGameSceneRenderBuilder
{
    void RegisterRenderPass(RenderTargetId target, int order, IRenderPass pass);

    void RegisterEmitter<TEmitter, TEntity>(int order)
        where TEmitter : DrawCommandEmitter<TEntity>, new()
        where TEntity : class;

    public void RegisterRenderer<TCommand, TRenderer>(DrawCommandTag commandTag, params DrawCommandId[] commandIds)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>;
}

public interface IGameSceneFeatureBuilder
{
    void RegisterDrawFeature<TEmitter, TFeature, TDrawData>(int order)
        where TFeature : class, IGameFeature, IDrawableFeature<TDrawData>, new()
        where TEmitter : DrawCommandEmitter<TDrawData>
        where TDrawData : class;

    void RegisterFeature<T>(int order) where T : IGameFeature, new();
}

public sealed class GameSceneConfigBuilder(IGraphicsDevice graphics, FeatureManager features, ModuleManager modules)
    : IGameSceneRenderBuilder, IGameSceneFeatureBuilder, IGameSceneModuleBuilder
{
    private readonly SortedList<int, Func<IGameFeature>> _features = new();
    private readonly SortedList<int, (Func<IDrawableFeature>, Type)> _drawFeatures = new();
    private readonly SortedList<int, RenderPassRegistryMeta> _passes = new();
    private readonly SortedList<int, Func<IDrawCommandEmitter>> _emitters = new();
    private readonly SortedList<int, Func<GameModule>> _modules = new();
    private readonly List<RendererRegistry> _renderers = new();

    internal void Clear()
    {
        _features.Clear();
        _emitters.Clear();
        _passes.Clear();
        _renderers.Clear();
    }

    public IGraphicsDevice GraphicsDevice { get; } = graphics;

    public FeatureManager FeatureManager { get; } = features;
    public ModuleManager ModuleManager { get; } = modules;

    public SortedList<int, Func<IGameFeature>> Features => _features;
    public SortedList<int, (Func<IDrawableFeature>, Type)> DrawFeatures => _drawFeatures;
    public SortedList<int, Func<IDrawCommandEmitter>> Emitters => _emitters;
    public SortedList<int, RenderPassRegistryMeta> Passes => _passes;
    public SortedList<int, Func<GameModule>> Modules => _modules;

    public List<RendererRegistry> Renderers => _renderers;


    public void RegisterFeature<T>(int order) where T : IGameFeature, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _features.Add(order, () => new T());
    }

    public void RegisterDrawFeature<TEmitter, TFeature, TDrawData>(int order)
        where TFeature : class, IGameFeature, IDrawableFeature<TDrawData>, new()
        where TEmitter : DrawCommandEmitter<TDrawData>
        where TDrawData : class
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));

        _drawFeatures.Add(order, (() => new TFeature(), typeof(TEmitter)));
    }

    public void RegisterEmitter<TEmitter, TDrawData>(int order)
        where TEmitter : DrawCommandEmitter<TDrawData>, new()
        where TDrawData : class
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _emitters.Add(order, () => new TEmitter());
    }

    public void RegisterRenderPass(RenderTargetId target, int order, IRenderPass pass)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _passes.Add(order, new RenderPassRegistryMeta(target, pass));
    }

    public void RegisterRenderer<TCommand, TRenderer>(DrawCommandTag commandTag, params DrawCommandId[] commandIds)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>
    {
        var registry = new RendererRegistry(commandIds, commandTag,
            (submitter, cmdId, tag) => submitter.Register<TCommand, TRenderer>(cmdId, tag));
        _renderers.Add(registry);
    }

    public record struct RenderPassRegistryMeta(RenderTargetId Target, IRenderPass Pass);

    public record  RendererRegistry(
        DrawCommandId[] CommandIds,
        DrawCommandTag CommandTag,
        Action<DrawCommandSubmitter, DrawCommandId, DrawCommandTag> Bind);

    public void RegisterModule<T>(int order) where T : GameModule, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        _modules.Add(order, () => new T());
    }
}