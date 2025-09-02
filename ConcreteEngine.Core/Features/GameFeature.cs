using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Core.Features;

public interface IGameFeature
{
    bool IsUpdateable { get; }
    void Initialize();
    void UpdateTick(int tick);
    void Update(in FrameMetaInfo frameCtx);
    void AttachContext(GameFeatureContext context);
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
    protected GameFeatureContext Context { get; private set; } = null!;

    public void AttachContext(GameFeatureContext context)
    {
        Context = context;
    }

    public abstract bool IsUpdateable { get; }
    public abstract void Initialize();
    public virtual void UpdateTick(int tick){}
    public virtual void Update(in FrameMetaInfo frameCtx){}
    public virtual void Unload(){}

}