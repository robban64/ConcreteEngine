namespace ConcreteEngine.Core.Module;

public abstract class GameModule
{
    private GameEngineContext _context = null!;
    
    public abstract bool IsUpdateable { get;  }
    public abstract bool IsRenderable { get;  }
    public int Order { get; internal set; }
    protected GameEngineContext Context => _context;

    internal void AttatchContext(GameEngineContext context, int order)
    {
        _context = context;
        Order = order;
    }
    
    public virtual void Update(float dt) {}
    public virtual void Render(float dt){}
    
    public virtual void Load(){}
    public virtual void Unload(){}

    
    /*
    protected void Publish(IGameCommand cmd)
        => Ctx.Publish(cmd);

    protected void Subscribe<TEvent>(Action<IGameEvent> handler) where TEvent : IGameEvent
    {
        var sub = Ctx.Subscribe<TEvent>(handler);
        _subscriptions.Add(sub);
    }
    */
}