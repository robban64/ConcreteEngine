namespace ConcreteEngine.Core.Features;

public interface IGameFeature
{
    void Initialize();
    void UpdateTick(int tick);
    void Update(in UpdateInfo frameCtx);
    void AttachContext(GameFeatureContext context);
    void Unload();
}

public abstract class GameFeature : IGameFeature
{
    protected GameFeatureContext Context { get; private set; } = null!;

    public void AttachContext(GameFeatureContext context)
    {
        Context = context;
    }

    public abstract void Initialize();

    public virtual void UpdateTick(int tick)
    {
    }

    public virtual void Update(in UpdateInfo frameCtx)
    {
    }

    public virtual void Unload()
    {
    }
}