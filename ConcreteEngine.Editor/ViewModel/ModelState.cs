#region

using ConcreteEngine.Common;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.ViewModel;

internal interface IModelState
{
    bool Active { get; }
    bool PendingRefresh { get; }
    void InvokeAction(TransitionKey action);
    void TriggerEvent<TEvent>(EventKey eventKey, TEvent eventData);
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

    public bool KeepAlive { get; }

    private ModelState(
        Func<T> factory,
        StateTransitionDel<T> onEnter,
        StateTransitionDel<T> onLeave,
        StateTransitionDel<T>? onRefresh = null,
        Dictionary<EventKey, object>? events = null,
        bool keepAlive = false)
    {
        _factory = factory;
        _onEnter = onEnter;
        _onLeave = onLeave;
        KeepAlive = keepAlive;
        _onRefresh = onRefresh;
        _events = events;
        
        if(KeepAlive) State = _factory();
    }

    public void ResetState()
    {
        Active = false;
        if(!KeepAlive)
            State = null;
    }


    public void EnqueueRefreshNextFrame()
    {
        if (!Active) return;
        PendingRefresh = true;
    }

    public bool TryInvokePendingRefresh()
    {
        if (!PendingRefresh || !Active) return false;
        if (_onRefresh is null)
        {
            PendingRefresh = false;
            ConsoleService.SendLog($"OnRefresh is null: Refresh failed for {typeof(T).Name}");
            return false;
        }

        InvokeAction(TransitionKey.Refresh);
        PendingRefresh = false;
        return true;
    }

    public void InvokeAction(TransitionKey action)
    {
        switch (action)
        {
            case TransitionKey.Enter:
                InvalidOpThrower.ThrowIf(!KeepAlive && Active, nameof(Active));
                Active = true;
                _onEnter(this, State ??= _factory());
                break;
            case TransitionKey.Leave:
                InvalidOpThrower.ThrowIf(!KeepAlive && !Active, nameof(Active));
                _onLeave(this, State!);
                Active = false;
                break;
            case TransitionKey.Refresh:
                InvalidOpThrower.ThrowIf(!KeepAlive && !Active, nameof(Active));
                InvalidOpThrower.ThrowIfNull(_onRefresh, nameof(_onRefresh));
                _onRefresh!(this, State!);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    public void TriggerEvent(EventKey eventKey)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is StateEmptyEventDel<T> del)
        {
            ConsoleService.SendLog($"Event triggered: {eventKey} for {typeof(T).Name}");
            del(this);
            return;
        }

        throw new ArgumentException(
            $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected {typeof(StateEmptyEventDel<T>).Name}");
    }

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent eventData)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));


        if (handler is StateEventDel<T, TEvent> del)
        {
            ConsoleService.SendLog($"Event triggered: {eventKey} for {typeof(T).Name} with {typeof(TEvent).Name}");
            del(this, eventData);
            return;
        }

        throw new ArgumentException(
            $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected {typeof(StateEventDel<T, TEvent>).Name}");
    }

    public static ViewModelStateBuilder CreateBuilder(Func<T> factory) => new(factory);

    public class ViewModelStateBuilder(Func<T> factory)
    {
        private StateTransitionDel<T>? _onEnter;
        private StateTransitionDel<T>? _onLeave;
        private StateTransitionDel<T>? _onRefresh;
        private Dictionary<EventKey, object>? _events;

        private bool _keepAlive = false;

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

        public ViewModelStateBuilder RegisterEvent(EventKey eventKey, StateEmptyEventDel<T> handler)
        {
            _events ??= new Dictionary<EventKey, object>();
            _events.Add(eventKey, handler);
            return this;
        }

        public ViewModelStateBuilder RegisterEvent<TEvent>(EventKey eventKey, StateEventDel<T, TEvent> handler)
        {
            _events ??= new Dictionary<EventKey, object>();
            _events.Add(eventKey, handler);
            return this;
        }

        public ViewModelStateBuilder KeepAlive()
        {
            _keepAlive = true;
            return this;
        }

        public ModelState<T> Build()
        {
            InvalidOpThrower.ThrowIfNull(factory, nameof(factory));
            InvalidOpThrower.ThrowIfNull(_onEnter, nameof(_onEnter));
            InvalidOpThrower.ThrowIfNull(_onLeave, nameof(_onLeave));
            return new ModelState<T>(factory, _onEnter!, _onLeave!, _onRefresh, _events, _keepAlive);
        }
    }
}