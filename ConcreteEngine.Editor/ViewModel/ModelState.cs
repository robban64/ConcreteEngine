using ConcreteEngine.Common;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.ViewModel;

internal static class ModelCtx<T> where T : class
{
    internal static Func<T> Factory { get; set; }
    internal static Action<T>? StateDestructor { get; set; }
    internal static StateTransitionDel<T> OnEnter { get; set; }
    internal static StateTransitionDel<T> OnLeave { get; set; }
    internal static StateTransitionDel<T>? OnRefresh { get; set; }

    public static T? State { get; private set; }

    public static T CreateState() => State = Factory();

    public static void ResetState()
    {
        StateDestructor?.Invoke(State!);
        State = null;
    }
}

internal sealed class ModelStateDelegateBundle<T>(
    Func<T> factory,
    Action<T> stateDestructor,
    StateTransitionDel<T> onEnter,
    StateTransitionDel<T> onLeave,
    StateTransitionDel<T>? onRefresh = null
) where T : class
{
    public Func<T> Factory { get; } = factory;
    public Action<T> StateDestructor { get; } = stateDestructor;
    public StateTransitionDel<T> OnEnter { get; } = onEnter;
    public StateTransitionDel<T> OnLeave { get; } = onLeave;
    public StateTransitionDel<T>? OnRefresh { get; } = onRefresh;
}

internal interface IModelState
{
    bool Active { get; }
    bool PendingRefresh { get; }
    void InvokeAction(TransitionKey action);
    void TriggerEvent<TEvent>(EventKey eventKey, in TEvent eventData);
}

public sealed class NoOpEvent
{
    public static NoOpEvent Instance { get; } = new NoOpEvent();

    private NoOpEvent()
    {
    }
}

internal sealed class ModelState<T> : IModelState where T : class
{
    private readonly Func<T> _factory;
    private readonly StateTransitionDel<T> _onEnter;
    private readonly StateTransitionDel<T> _onLeave;
    private readonly StateTransitionDel<T>? _onRefresh;

    private readonly Dictionary<EventKey, object>? _events;

    public T? State { get; private set; }
    public bool Active { get; private set; }
    public bool PendingRefresh { get; private set; } = false;

    private ModelState(
        Func<T> factory,
        StateTransitionDel<T> onEnter,
        StateTransitionDel<T> onLeave,
        StateTransitionDel<T>? onRefresh = null,
        Dictionary<EventKey, object>? events = null)
    {
        _factory = factory;
        _onEnter = onEnter;
        _onLeave = onLeave;
        _onRefresh = onRefresh;
        _events = events;
    }

    public void ResetState()
    {
        Active = false;
        State = null;
    }

    public void EnqueueRefreshNextFrame()
    {
        if (PendingRefresh) return;
        PendingRefresh = true;
    }

    public bool TryInvokePendingRefresh()
    {
        if (!PendingRefresh) return false;
        if (_onRefresh is null)
        {
            PendingRefresh = false;
            ConsoleService.SendLog($"OnRefresh is null: Refresh failed for {typeof(T).Name}");
            return false;
        }
        _onRefresh!(this, State!);
        InvokeAction(TransitionKey.Refresh);
        PendingRefresh = false;
        return true;
    }

    public void InvokeAction(TransitionKey action)
    {
        switch (action)
        {
            case TransitionKey.Enter:
                InvalidOpThrower.ThrowIf(Active, nameof(Active));
                Active = true;
                _onEnter(this, State ??= _factory());
                break;
            case TransitionKey.Leave:
                InvalidOpThrower.ThrowIfNot(Active, nameof(Active));
                _onLeave(this, State!);
                Active = false;
                break;
            case TransitionKey.Refresh:
                InvalidOpThrower.ThrowIfNot(Active, nameof(Active));
                InvalidOpThrower.ThrowIfNull(_onRefresh, nameof(_onRefresh));
                _onRefresh!(this, State!);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    public unsafe void TriggerEvent(EventKey eventKey)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is not EventEntry<NoOpEvent> entry)
            throw new ArgumentException(
                $"{eventKey} was invoked with invalid type: actual {nameof(NoOpEvent)}, expected {nameof(EventEntry<NoOpEvent>.EventType.Name)}");

        entry.Handler(this, NoOpEvent.Instance);
    }

    public unsafe void TriggerEvent<TEvent>(EventKey eventKey, in TEvent eventData)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is not EventEntry<TEvent> entry)
            throw new ArgumentException(
                $"{eventKey} was invoked with invalid type: actual {typeof(TEvent).Name}, expected {nameof(EventEntry<TEvent>.EventType.Name)}");

        ConsoleService.SendLog("Event triggered: " + typeof(TEvent).Name);
        entry.Handler(this, in eventData);
    }

    public static ViewModelStateBuilder CreateBuilder(Func<T> factory) => new(factory);

    public class ViewModelStateBuilder(Func<T> factory)
    {
        private StateTransitionDel<T>? _onEnter;
        private StateTransitionDel<T>? _onLeave;
        private StateTransitionDel<T>? _onRefresh;
        private Dictionary<EventKey, object>? _events;

        public ViewModelStateBuilder OnEnter(StateTransitionDel<T> handler)
        {
            _onEnter = handler;
            return this;
        }

        public ViewModelStateBuilder OnLeave(StateTransitionDel<T> handler)
        {
            _onLeave = handler;
            return this;
        }

        public ViewModelStateBuilder OnRefresh(StateTransitionDel<T> handler)
        {
            _onRefresh = handler;
            return this;
        }

/*
        public ViewModelStateBuilder RegisterEvent<TEvent>(EventKey eventKey, StateEventDel<T, TEvent> handler)
        {
            _events ??= new Dictionary<EventKey, object>();
            _events.Add(eventKey, new EventEntry<TEvent>(handler));
            return this;
        }
*/
        public unsafe ViewModelStateBuilder RegisterEventNoOp(EventKey eventKey,
            delegate*<ModelState<T>, in NoOpEvent, void> handler)
        {
            return RegisterEvent<NoOpEvent>(eventKey, handler);
        }

        public unsafe ViewModelStateBuilder RegisterEvent<TEvent>(EventKey eventKey,
            delegate*<ModelState<T>, in TEvent, void> handler)
        {
            _events ??= new Dictionary<EventKey, object>();
            _events.Add(eventKey, new EventEntry<TEvent>(handler));
            return this;
        }

        public ModelState<T> Build()
        {
            InvalidOpThrower.ThrowIfNull(factory, nameof(factory));
            InvalidOpThrower.ThrowIfNull(_onEnter, nameof(_onEnter));
            InvalidOpThrower.ThrowIfNull(_onLeave, nameof(_onLeave));
            return new ModelState<T>(factory, _onEnter!, _onLeave!, _onRefresh, _events);
        }
    }

    private sealed unsafe class EventEntry<TEvent>(delegate*<ModelState<T>, in TEvent, void> handler)
    {
        public readonly delegate*<ModelState<T>, in TEvent, void> Handler = handler;
        public static Type EventType => typeof(TEvent);
    }
}