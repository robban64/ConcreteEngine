namespace ConcreteEngine.Core;

public abstract class GameFeature
{
    private GameEngineContext _context = null!;
    
    public abstract bool IsUpdateable { get;  }
    public int Order { get; internal set; }
    protected GameEngineContext Context => _context;
    
    public abstract void Update(float dt);
    public abstract void Load();
    public abstract void Unload();

    internal void AttatchContext(GameEngineContext context, int order)
    {
        _context = context;
        Order = order;
    }


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