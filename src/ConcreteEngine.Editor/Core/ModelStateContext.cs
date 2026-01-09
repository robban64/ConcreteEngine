using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ModelStateContext
{
    public static ViewModelStateBuilder CreateBuilder() => new();

    private static ReadOnlySpan<string> EventKeys => EnumCache<EventKey>.NameSpan;

    private readonly Action<ModelStateContext> _onEnter;
    private readonly Action<ModelStateContext> _onLeave;
    private readonly Action<ModelStateContext>? _onRefresh;

    private readonly StateEntry _stateEntry;

    private readonly Dictionary<EventKey, Delegate>? _events;

    public bool Active { get; private set; }
    public bool PendingRefresh { get; private set; }


    private ModelStateContext(
        StateEntry stateEntry,
        Action<ModelStateContext> onEnter,
        Action<ModelStateContext> onLeave,
        Action<ModelStateContext> onRefresh,
        Dictionary<EventKey, Delegate>? events = null)
    {
        _stateEntry = stateEntry;
        _onEnter = onEnter;
        _onLeave = onLeave;
        _onRefresh = onRefresh;
        _events = events;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawLeft() => _stateEntry.InvokeDrawLeft();
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawRight() => _stateEntry.InvokeDrawRight();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnqueueRefreshNextFrame()
    {
        if (!Active || PendingRefresh || _onRefresh is null) return;
        ModelManager.EnqueueRefresh(this);
        PendingRefresh = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TryInvokePendingRefresh()
    {
        if (!PendingRefresh || !Active) return;
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

        if (handler is not Action del)
        {
            throw new ArgumentException(
                $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected empty event");
        }

        del();
    }

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent eventData)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is not Action<TEvent> del)
        {
            throw new ArgumentException(
                $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected {typeof(TEvent).Name}");
        }

        del(eventData);
    }


    private abstract class StateEntry(ComponentDrawKind drawKind)
    {
        public ComponentDrawKind DrawKind = drawKind;
        public bool LeftActive { get; set; }
        public bool RightActive { get; set; }
        
        public abstract void InvokeDrawLeft();
        public abstract void InvokeDrawRight();
        
        public abstract void Clear();
    }

    private sealed class StateEntry<TState>(ComponentDrawKind drawKind) : StateEntry(drawKind)
    {
        public required TState State;

        public Action<TState>? DrawLeft;
        public Action<TState>? DrawRight;

        public override void InvokeDrawLeft() => DrawLeft!(State);
        public override void InvokeDrawRight() => DrawRight!(State);

        public override void Clear() => State = default!;
    }

    public sealed class ViewModelStateBuilder
    {
        private StateEntry _stateEntry = null!;
        private Action<ModelStateContext> _onEnter = null!;
        private Action<ModelStateContext> _onLeave = null!;
        private Action<ModelStateContext> _onRefresh = null!;
        private readonly Dictionary<EventKey, Delegate> _events = [];

        public ViewModelStateBuilder MakeState<TState>(
            ComponentDrawKind kind,
            Action<TState> draw,
            Action<TState>? drawSecondary = null
        ) where TState : class, new()
        {
            InvalidOpThrower.ThrowIfNotNull(_stateEntry, nameof(_stateEntry));
            var entry = new StateEntry<TState>(kind) { DrawKind = kind, State = new TState() };
            switch (kind)
            {
                case ComponentDrawKind.Left: entry.DrawLeft = draw; break;
                case ComponentDrawKind.Right: entry.DrawRight = draw; break;
                case ComponentDrawKind.Both:
                    {
                        ArgumentNullException.ThrowIfNull(drawSecondary);
                        entry.DrawLeft = draw;
                        entry.DrawRight = drawSecondary;
                        break;
                    }
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            _stateEntry = entry;

            return this;
        }

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
            _events.Add(eventKey, handler);
            return this;
        }

        public ViewModelStateBuilder RegisterEvent<TEvent>(EventKey eventKey, Action<TEvent> handler)
        {
            _events.Add(eventKey, handler);
            return this;
        }


        public ModelStateContext Build()
        {
            InvalidOpThrower.ThrowIfNull(_onEnter, nameof(_onEnter));
            InvalidOpThrower.ThrowIfNull(_onLeave, nameof(_onLeave));
            InvalidOpThrower.ThrowIfNull(_stateEntry, nameof(_stateEntry));
            return new ModelStateContext(_stateEntry, _onEnter, _onLeave, _onRefresh, _events);
        }
    }
}

/*
 *     private sealed class EventEntry<T>
   {
       public required EventKey Key;
       public required Action<T> EventDel;
   }
*/