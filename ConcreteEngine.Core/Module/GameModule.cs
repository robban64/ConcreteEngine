namespace ConcreteEngine.Core.Module;

public abstract class GameModule
{
    private readonly GameEngineContext _context;
    private readonly int _order;
    
    public abstract bool IsUpdateable { get;  }
    public abstract bool IsRenderable { get;  }
    
    public int Order => _order;
    protected GameEngineContext Context => _context;

    protected GameModule(GameEngineContext context, int order)
    {
        _context = context;
        _order = order;
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