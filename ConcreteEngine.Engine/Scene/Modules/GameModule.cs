#region

#endregion

namespace ConcreteEngine.Engine.Scene.Modules;

public abstract class GameModule
{
    public int Order { get; internal set; }
    protected GameModuleContext Context { get; private set; } = null!;

    protected GameModule()
    {
    }

    public abstract void Initialize();

    public virtual void UpdateTick(float deltaTime)
    {
    }
    public virtual void OnSceneReady()
    {
    }

    public virtual void OnSceneUnload()
    {
    }

    public virtual void Unload()
    {
    }

    internal void AttachContext(GameModuleContext context)
    {
        Context = context;
    }
}