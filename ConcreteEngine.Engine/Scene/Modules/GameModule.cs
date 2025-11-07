#region

using ConcreteEngine.Engine.Data;

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

    public virtual void UpdateTick(int tick)
    {
    }

    public virtual void Update(in UpdateTickInfo frameCtx)
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