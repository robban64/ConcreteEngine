using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ModelStateContext
{
    private readonly Action<ModelStateContext> _onEnter;
    private readonly Action<ModelStateContext> _onLeave;
    private readonly Action<ModelStateContext>? _onRefresh;

    private readonly Dictionary<EventKey, object>? _events;

    public bool Active { get; private set; }
    public bool PendingRefresh { get; private set; } = false;


    private ModelStateContext(
        Action<ModelStateContext> onEnter,
        Action<ModelStateContext> onLeave,
        Action<ModelStateContext>? onRefresh = null,
        Dictionary<EventKey, object>? events = null)
    {
        _onEnter = onEnter;
        _onLeave = onLeave;
        _onRefresh = onRefresh;
        _events = events;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnqueueRefreshNextFrame()
    {
        if (!Active) return;
        PendingRefresh = true;
    }

    public void TryInvokePendingRefresh()
    {
        if (!PendingRefresh || !Active) return;
        if (_onRefresh is null)
        {
            PendingRefresh = false;
            ConsoleService.SendLog($"OnRefresh is null");
            return;
        }

        InvokeAction(TransitionKey.Refresh);
        PendingRefresh = false;
    }

    public void InvokeAction(TransitionKey action)
    {
        switch (action)
        {
            case TransitionKey.Enter:
                Active = true;
                _onEnter(this);
                break;
            case TransitionKey.Leave:
                _onLeave(this);
                Active = false;
                break;
            case TransitionKey.Refresh:
                InvalidOpThrower.ThrowIfNull(_onRefresh, nameof(_onRefresh));
                _onRefresh!(this);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    public void TriggerEvent(EventKey eventKey)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is Action del)
        {
            ConsoleService.SendLog($"Event triggered: {eventKey}");
            del();
            return;
        }

        throw new ArgumentException(
            $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected empty event");
    }

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent eventData)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));


        if (handler is Action<TEvent> del)
        {
            ConsoleService.SendLog($"Event triggered: {eventKey} with {typeof(TEvent).Name}");
            del(eventData);
            return;
        }

        throw new ArgumentException(
            $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected {typeof(TEvent).Name}");
    }

    public static ViewModelStateBuilder CreateBuilder() => new();

    public class ViewModelStateBuilder
    {
        private Action<ModelStateContext>? _onEnter;
        private Action<ModelStateContext>? _onLeave;
        private Action<ModelStateContext>? _onRefresh;
        private Dictionary<EventKey, object>? _events;

        public ViewModelStateBuilder OnEnter(Action<ModelStateContext> handler)
        {
            _onEnter = handler;
            return this;
        }

        public ViewModelStateBuilder OnLeave(Action<ModelStateContext> handler)
        {
            _onLeave = handler;
            return this;
        }

        public ViewModelStateBuilder OnRefresh(Action<ModelStateContext> handler)
        {
            _onRefresh = handler;
            return this;
        }

        public ViewModelStateBuilder RegisterEvent(EventKey eventKey, Action handler)
        {
            _events ??= new Dictionary<EventKey, object>();
            _events.Add(eventKey, handler);
            return this;
        }

        public ViewModelStateBuilder RegisterEvent<TEvent>(EventKey eventKey, Action<TEvent> handler)
        {
            _events ??= new Dictionary<EventKey, object>();
            _events.Add(eventKey, handler);
            return this;
        }


        public ModelStateContext Build()
        {
            InvalidOpThrower.ThrowIfNull(_onEnter, nameof(_onEnter));
            InvalidOpThrower.ThrowIfNull(_onLeave, nameof(_onLeave));
            return new ModelStateContext(_onEnter!, _onLeave!, _onRefresh, _events);
        }
    }
}