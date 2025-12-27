namespace ConcreteEngine.Engine.Scene.Modules;

public abstract class GameModule
{
    public int Id { get; internal set; }
    protected GameModuleContext Context { get; private set; } = null!;

    protected GameModule()
    {
    }

    public virtual void UpdateTick(float deltaTime)
    {
    }

    public virtual void OnStart()
    {
    }

    public virtual void OnDestroy()
    {
    }

    internal void AttachContext(GameModuleContext context) => Context = context;
}