namespace ConcreteEngine.Core.Module;

/*


public sealed class ComponentService
{
    private readonly SortedList<int, SortedList<int, IComponentDrawHandler>> _drawHandlers = new(4);
    private readonly TypeRegistryCollection<List<GameComponent>> _drawRegistry = new ();

    private readonly TypeRegistryCollection<IDrawCommandEmitter> _registeredEmitters = new();

    private readonly SortedList<int, GameComponent> _components = new(64);

    private readonly PendingListDouble<GameComponent> _pendingComponents;
    //private readonly List<GameComponent> _pendingAddComponents = new(16);
    //private readonly List<GameComponent> _pendingRemoveComponents = new(16);

    private readonly DrawCommandCollector _commandCollector = new();
    private readonly DrawCommandSubmitter _commandSubmitter = new();

    protected GameEngineContext Context { get; }


    public ComponentService(GameEngineContext context)
    {
        Context = context;
        AddDrawHandler<SpriteComponent, SpriteDrawHandler>(new SpriteDrawHandler(0, 0));

        _pendingComponents = new PendingListDouble<GameComponent>(
            it => _components.Remove(it.Id),
            it => _components.Add(it.Id, it)
        );
    }

    public void AddComponent<TComponent>(TComponent component) where TComponent : GameComponent
    {
        _components.Add(component.DrawOrder, component);
    }
    
    public void AddDrawHandler<TComponent, THandler>(THandler handler) 
        where TComponent : GameComponent
        where THandler : class, IComponentDrawHandler
    {
        _drawRegistry.Register<TComponent>(new List<GameComponent>(8));
        
        if(!_drawHandlers.ContainsKey(handler.Target))
            _drawHandlers.Add(handler.Target, new SortedList<int, IComponentDrawHandler>(4));
        
        _drawHandlers[handler.Target].Add(handler.Order, handler);
    }

    public void Register<TEmit, TCommand>(TEmit emitter) 
        where TEmit : class, IDrawCommandEmitter
        where TCommand : unmanaged, IDrawCommand
    {
        _registeredEmitters.Register<TEmit>(emitter);
        _commandCollector.RegisterEmitter<TEmit>(emitter);
        _commandSubmitter.RegisterCommand<TCommand>();
    }


    public void Update(float dt)
    {
        _pendingComponents.Flush();

        
        foreach (var component in _components.Values)
        {
            component.Update(dt);
        }
    }

    public void Render(float dt)
    {
        var graphics = Context.Graphics;
        var pipeline = graphics.RenderPipeline;

        foreach (var components in _drawRegistry)
        {
            components.Value.Clear();
        }
        
        foreach (var component in _components.Values)
        {
            _drawRegistry.Get(component.GetType()).Add(component);
        }

        foreach (var (target, drawHandlers) in _drawHandlers)
        {
            pipeline.BindRenderPass(target);
            foreach (var handler in drawHandlers.Values)
            {
                handler.Emit(graphics);
            }
        }
    }
}
*/