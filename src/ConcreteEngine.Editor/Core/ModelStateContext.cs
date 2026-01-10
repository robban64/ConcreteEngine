using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ModelStateContext
{
    public static Builder<TState> CreateBuilder<TState>(ComponentDrawKind kind) 
        where TState : class, new() => new(kind);

    private static ReadOnlySpan<string> EventKeys => EnumCache<EventKey>.NameSpan;

    public bool Active { get; private set; }
    public bool PendingRefresh { get; private set; }


    private readonly Dictionary<EventKey, Delegate>? _events;

    private readonly StateEntry _stateEntry;


    private ModelStateContext(
        StateEntry stateEntry,
        Dictionary<EventKey, Delegate>? events = null)
    {
        _stateEntry = stateEntry;
        _events = events;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawLeft() => _stateEntry.DrawLeft();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawRight() => _stateEntry.DrawRight();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnqueueRefreshNextFrame()
    {
        if (!Active || PendingRefresh) return;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if (Active) return;
        Active = true;
        _stateEntry.Enter(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Leave()
    {
        if (!Active) return;
        _stateEntry.Leave(this);
        Active = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Refresh()
    {
        if (!Active) return;
        _stateEntry.Refresh(this);
    }


    public void InvokeAction(TransitionKey action)
    {
        switch (action)
        {
            case TransitionKey.Enter: Enter(); break;
            case TransitionKey.Leave: Leave(); break;
            case TransitionKey.Refresh: Refresh(); break;
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

        public abstract void DrawLeft();
        public abstract void DrawRight();

        public abstract void Enter(ModelStateContext ctx);
        public abstract void Leave(ModelStateContext ctx);
        public abstract void Refresh(ModelStateContext ctx);

        public abstract void Clear();
    }

    private sealed class StateEntry<TState>(
        ComponentDrawKind drawKind,
        Action<TState>? drawLeft,
        Action<TState>? drawRight,
        Action<TState, ModelStateContext>? onEnter,
        Action<TState, ModelStateContext>? onLeave,
        Action<TState, ModelStateContext>? onRefresh) : StateEntry(drawKind) where TState : class, new()
    {
        public required TState State;

        public override void Enter(ModelStateContext ctx) => onEnter?.Invoke(State, ctx);
        public override void Leave(ModelStateContext ctx) => onLeave?.Invoke(State, ctx);
        public override void Refresh(ModelStateContext ctx) => onRefresh?.Invoke(State, ctx);

        public override void DrawLeft() => drawLeft?.Invoke(State);
        public override void DrawRight() => drawRight?.Invoke(State);

        public override void Clear() => State = null!;
    }

    public class Builder<TState>(ComponentDrawKind kind) where TState : class, new()
    {
        private Action<TState>? _drawLeft;
        private Action<TState>? _drawRight;

        private Action<TState, ModelStateContext>? _onEnter;
        private Action<TState, ModelStateContext>? _onLeave;
        private Action<TState, ModelStateContext>? _onRefresh;
        private readonly Dictionary<EventKey, Delegate> _events = new();

        public Builder<TState> MakeState(Action<TState> draw, Action<TState>? drawSecondary = null)
        {
            ArgumentNullException.ThrowIfNull(draw);

            switch (kind)
            {
                case ComponentDrawKind.Left: _drawLeft = draw; break;
                case ComponentDrawKind.Right: _drawRight = draw; break;
                case ComponentDrawKind.Both:
                    {
                        ArgumentNullException.ThrowIfNull(drawSecondary);
                        _drawLeft = draw;
                        _drawRight = drawSecondary;
                        break;
                    }
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            return this;
        }

        public Builder<TState> RegisterEvent(EventKey eventKey, Action handler)
        {
            _events.Add(eventKey, handler);
            return this;
        }

        public Builder<TState> RegisterEvent<TEvent>(EventKey eventKey, Action<TEvent> handler)
        {
            _events.Add(eventKey, handler);
            return this;
        }

        public Builder<TState> OnRefresh(Action<TState, ModelStateContext> handler)
        {
            _onRefresh = handler;
            return this;
        }

        public Builder<TState> OnEnter(Action<TState, ModelStateContext> handler)
        {
            _onEnter = handler;
            return this;
        }

        public Builder<TState> OnLeave(Action<TState, ModelStateContext> handler)
        {
            _onLeave = handler;
            return this;
        }

        public ModelStateContext Build()
        {
            var entry = new StateEntry<TState>(kind, _drawLeft, _drawRight, _onEnter, _onLeave, _onRefresh)
            {
                State = new TState()
            };
            return new ModelStateContext(entry, _events);
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