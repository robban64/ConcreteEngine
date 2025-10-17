namespace ConcreteEngine.Core.Stuff;
/*

internal abstract class BaseEngineState
{
    public Type? Next { get; private set; }
    public bool Entered { get; private set; }

    protected EngineCoreContext Context { get; private set; }

    public void AttachState(Type next, EngineCoreContext  context)
    {
        Next = next;
        Entered = true;
        Context = context;
        OnEnter();
    }

    public Type? DetachState()
    {
        var next = Next;
        OnExit();
        Next = null;
        return next;
    }

    public abstract bool ShouldAdvance();

    protected virtual void OnEnter(){}

    public virtual void OnUpdate(in FrameMetaInfo frameCtx)
    {
    }

    public virtual void OnRender(in FrameMetaInfo frameCtx)
    {
    }

    protected virtual void OnExit()
    {
    }
}

internal sealed class EngineStateMachine
{
    private readonly Dictionary<Type, Func<(Type, BaseEngineState)>> _registry = new(4);

    //private readonly Stack<BaseEngineState> _stack = new();
    private int _version;

    private BaseEngineState? _currentState = null;
    private EngineCoreContext _context = null!;


    public void RegisterState<T, TNext>()
        where T : BaseEngineState, new()
        where TNext : BaseEngineState =>
        _registry.Add(typeof(T), New<T, TNext>);

    private static (Type, BaseEngineState) New<T, TNext>() where T : BaseEngineState, new()
        where TNext : BaseEngineState =>
        (typeof(TNext), new T());


    public void Start<T>(EngineCoreContext context) where T : BaseEngineState, new()
    {
        var (nextStateType, state) = GetByRef(typeof(T));
        _currentState = state;
        _currentState.AttachState(nextStateType, _context);
    }

    public void Update(in FrameMetaInfo frameCtx)
    {
        if (_currentState == null) return;
        var state = _currentState;
        var startVersion = _version;

        if (!state.Entered)
        {
            if (startVersion != _version) return;
        }

        state.OnUpdate(in frameCtx);
        if (startVersion != _version) return;

        if (state.ShouldAdvance())
        {
            AdvanceToNext();
        }
    }

    private void AdvanceToNext()
    {
        var newStateType = _currentState!.DetachState();
        if (newStateType != null)
        {
            var (nextStateType, currentState) = GetByRef(newStateType);
            _currentState = currentState;
            _currentState.AttachState(nextStateType, _context);
        }

        _version++;
    }

    private (Type, BaseEngineState) GetByRef(Type t)
    {
        if (!_registry.TryGetValue(t, out var factory))
            throw new InvalidOperationException($"Unknown state type '{t.Name}'.");

        return factory();
    }


}
*/