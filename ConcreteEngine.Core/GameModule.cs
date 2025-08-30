using ConcreteEngine.Graphics.Data;

namespace ConcreteEngine.Core;

public abstract class GameModule
{
    public int Order { get; private set; }
    protected GameModuleContext Context { get; private set; } = null!;
    
    protected GameModule(){}
    
    public abstract void Initialize();
    public virtual void UpdateTick(int tick){}
    public virtual void Update(in FrameMetaInfo frameCtx) {}
    public virtual void OnSceneReady() { }
    public virtual void OnSceneUnload() { }

    public virtual void Unload(){}
    
    internal void AttachContext(GameModuleContext context, int order)
    {
        Context = context;
        Order = order;
    }

}