using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ModelStateComponent
{
    private static ReadOnlySpan<string> EventKeys => EnumCache<EventKey>.NameSpan;

    public static GlobalContext? TempGlobalContext;

    public static Builder<TState, TComponent> CreateBuilder<TState, TComponent>(ComponentDrawKind kind)
        where TState : class, new() where TComponent : EditorComponent<TState>, new() =>
        new(kind);

    public bool Active { get; private set; }

    private readonly Dictionary<EventKey, Delegate>? _events;

    private readonly StateObject _stateObject;
    private readonly GlobalContext _globalContext;

    private ModelStateComponent(
        StateObject stateObject,
        Dictionary<EventKey, Delegate>? events = null)
    {
        _globalContext = TempGlobalContext ?? throw new InvalidOperationException(nameof(TempGlobalContext));
        _stateObject = stateObject;
        _events = events;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawLeft() => _stateObject.DrawLeft();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawRight() => _stateObject.DrawRight();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Refresh()
    {
        if (!Active) return;
        _stateObject.Refresh(this);
    }

    public void Enter()
    {
        if (Active) return;
        Active = true;
        _stateObject.Enter(this);
    }

    public void Leave()
    {
        if (!Active) return;
        _stateObject.Leave(this);
        Active = false;
    }


    public void TriggerEvent(EventKey eventKey)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is not Action<GlobalContext> del)
        {
            throw new ArgumentException(
                $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected empty event");
        }

        del(_globalContext);
    }

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent eventData)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is not Action<GlobalContext, TEvent> del)
        {
            throw new ArgumentException(
                $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected {typeof(TEvent).Name}");
        }

        del(_globalContext, eventData);
    }


    private abstract class StateObject(ComponentDrawKind drawKind)
    {
        public ComponentDrawKind DrawKind = drawKind;

        public abstract void DrawLeft();
        public abstract void DrawRight();

        public abstract void Enter(ModelStateComponent ctx);
        public abstract void Leave(ModelStateComponent ctx);
        public abstract void Refresh(ModelStateComponent ctx);

        public abstract void Clear();
    }

    private sealed class StateObject<TState, TComponent>(
        ComponentDrawKind drawKind,
        Action<ModelStateComponent, TState>? onEnter,
        Action<ModelStateComponent, TState>? onLeave,
        Action<ModelStateComponent, TState>? onRefresh) : StateObject(drawKind)
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        public TState State = null!;
        public TComponent Component = null!;

        public override void Enter(ModelStateComponent ctx) => onEnter?.Invoke(ctx, State);
        public override void Leave(ModelStateComponent ctx) => onLeave?.Invoke(ctx, State);
        public override void Refresh(ModelStateComponent ctx) => onRefresh?.Invoke(ctx, State);

        public override void DrawLeft() => Component.DrawLeft(State);
        public override void DrawRight() => Component.DrawRight(State);

        public override void Clear() => State = null!;
    }

    public class Builder<TState, TComponent>(ComponentDrawKind kind)
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        private Action<ModelStateComponent, TState>? _onEnter;
        private Action<ModelStateComponent, TState>? _onLeave;
        private Action<ModelStateComponent, TState>? _onRefresh;

        private readonly Dictionary<EventKey, Delegate> _events = new();

/*
        public Builder<TState> BindDraw(Action<ModelStateComponent,TState>? drawLeft, Action<ModelStateComponent,TState>? drawRight = null)
        {
            ArgumentNullException.ThrowIfNull(draw);
            return this;
        }
*/
        public Builder<TState, TComponent> RegisterEvent(EventKey eventKey, Action<GlobalContext> handler)
        {
            _events.Add(eventKey, handler);
            return this;
        }

        public Builder<TState, TComponent> RegisterEvent<TEvent>(EventKey eventKey,
            Action<GlobalContext, TEvent> handler)
        {
            _events.Add(eventKey, handler);
            return this;
        }

        public Builder<TState, TComponent> OnRefresh(Action<ModelStateComponent, TState>? handler)
        {
            _onRefresh = handler;
            return this;
        }

        public Builder<TState, TComponent> OnEnter(Action<ModelStateComponent, TState>? handler)
        {
            _onEnter = handler;
            return this;
        }

        public Builder<TState, TComponent> OnLeave(Action<ModelStateComponent, TState>? handler)
        {
            _onLeave = handler;
            return this;
        }

        public ModelStateComponent Build()
        {
            var entry = new StateObject<TState, TComponent>(kind, _onEnter, _onLeave, _onRefresh);
            var result = new ModelStateComponent(entry, _events);
            entry.Component = EditorComponent<TState>.Make<TComponent>(result);
            entry.State = new TState();
            return result;
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