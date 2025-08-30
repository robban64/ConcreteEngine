using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Graphics.Data;

namespace ConcreteEngine.Core;

public interface IGameFeature
{
    bool IsUpdateable { get; }
    int Order { get; }
    void Initialize();
    void CollectFrame(ISceneNodeCollector collector);
    void UpdateTick(int tick);
    void Update(in FrameMetaInfo frameCtx);
    void AttachContext(GameFeatureContext context, int order);
    void Unload();
}

public interface IDrawableFeature : IGameFeature
{
    bool IsDrawable { get; set; }
    int DrawOrder { get; set; }
}

public interface IDrawableFeature<out T> : IDrawableFeature
{
    public T GetDrawables();
}

public abstract class GameFeature : IGameFeature
{
    public int Order { get; private set; }
    protected GameFeatureContext Context { get; private set; } = null!;

    public void AttachContext(GameFeatureContext context, int order)
    {
        Context = context;
        Order = order;
    }

    public abstract bool IsUpdateable { get; }
    public abstract void Initialize();
    public abstract void CollectFrame(ISceneNodeCollector collector);

    public abstract void UpdateTick(int tick);
    public abstract void Update(in FrameMetaInfo frameCtx);

    public virtual void Unload()
    {
    }
}