using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ModelStateComponent
{
    private static ReadOnlySpan<string> EventKeys => EnumCache<EventKey>.NameSpan;

    public static Builder<TState, TComponent> CreateBuilder<TState, TComponent>(GlobalContext ctx,
        DeferredEventDispatcher dispatcher)
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        return new Builder<TState, TComponent>(ctx, dispatcher);
    }

    public bool Active { get; private set; }

    private readonly StateObject _stateObject;
    private readonly DeferredEventDispatcher _dispatcher;
    private readonly GlobalContext _globalContext;

    private ModelStateComponent(StateObject stateObject, DeferredEventDispatcher dispatcher,
        GlobalContext globalContext)
    {
        _stateObject = stateObject;
        _dispatcher = dispatcher;
        _globalContext = globalContext;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawLeft(in FrameContext ctx) => _stateObject.DrawLeft(in ctx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawRight(in FrameContext ctx) => _stateObject.DrawRight(in ctx);

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
        _stateObject.MakeState();
        _stateObject.Enter(this);
    }

    public void Leave()
    {
        if (!Active) return;
        _stateObject.Leave(this);
        Active = false;
    }

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent evt)
    {
        if (!_events.TryGetValue(eventKey, out var handler))
            throw new KeyNotFoundException(nameof(eventKey));

        if (handler is not Action<GlobalContext, object, TEvent> evtHandler)
        {
            throw new ArgumentException(
                $"{eventKey} was invoked with invalid type: actual {handler.GetType().Name}, expected {typeof(TEvent).Name}");
        }

        _eventQueue.Enqueue(_stateObject.MakeEvent(eventKey, evt, evtHandler));
    }


    internal abstract class StateObject
    {
        public abstract DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<GlobalContext, object, TEvent> handler);

        public abstract void DrawLeft(in FrameContext ctx);
        public abstract void DrawRight(in FrameContext ctx);

        public abstract void Enter(ModelStateComponent ctx);
        public abstract void Leave(ModelStateComponent ctx);
        public abstract void Refresh(ModelStateComponent ctx);

        public abstract void MakeState();
        public abstract void ClearState();
    }

    public sealed class StateObject<TState, TComponent>(
        Func<TState> factory,
        Action<ModelStateComponent, TState>? onEnter,
        Action<ModelStateComponent, TState>? onLeave,
        Action<ModelStateComponent, TState>? onRefresh) : StateObject
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        public TState State = null!;
        public TComponent Component = null!;

        public override void Enter(ModelStateComponent ctx) => onEnter?.Invoke(ctx, State);
        public override void Leave(ModelStateComponent ctx) => onLeave?.Invoke(ctx, State);
        public override void Refresh(ModelStateComponent ctx) => onRefresh?.Invoke(ctx, State);

        public override DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<GlobalContext, object, TEvent> handler)
            => new DeferredEvent<TState, TEvent>(evtKey, this, evt, handler);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void DrawLeft(in FrameContext ctx) => Component.DrawLeft(State, in ctx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void DrawRight(in FrameContext ctx) => Component.DrawRight(State, in ctx);

        public override void MakeState()
        {
            if (State != null!) return;
            State = factory();
        }

        public override void ClearState() => State = null!;
    }

    public sealed class Builder<TState, TComponent>(GlobalContext ctx, DeferredEventDispatcher dispatcher)
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        private Action<ModelStateComponent, TState>? _onEnter;
        private Action<ModelStateComponent, TState>? _onLeave;
        private Action<ModelStateComponent, TState>? _onRefresh;

        public Builder<TState, TComponent> RegisterEvent<TEvent>(EventKey eventKey,
            Action<GlobalContext, TState, TEvent> handler)
        {
            dispatcher.Register<TState,TEvent>(eventKey, ((context, stateObj, evt) => handler(context, (TState)stateObj, evt) ));
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
            var entry = new StateObject<TState, TComponent>(() => new TState(), _onEnter, _onLeave, _onRefresh);
            var result = new ModelStateComponent(entry, dispatcher,ctx);
            entry.Component = EditorComponent<TState>.Make<TComponent>(result);
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