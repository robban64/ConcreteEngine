using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ComponentRuntime
{
    private static ReadOnlySpan<string> EventKeys => EnumCache<EventKey>.NameSpan;
    internal static DeferredEventDispatcher? SetupDispatcher;
    internal static GlobalContext? SetupContext;

    public static Builder<TState, TComponent> CreateBuilder<TState, TComponent>()
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        return new Builder<TState, TComponent>();
    }

    public bool Active { get; private set; }

    private readonly StateObject _stateObject;
    private readonly DeferredEventDispatcher _dispatcher;
    private readonly GlobalContext _globalContext;

    private ComponentRuntime(StateObject stateObject)
    {
        if (SetupContext == null || SetupDispatcher == null) throw new InvalidOperationException();
        _stateObject = stateObject;
        _dispatcher = SetupDispatcher;
        _globalContext = SetupContext;
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

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent evt) =>
        _dispatcher.EnqueueEvent(eventKey, _stateObject, evt);


    internal abstract class StateObject
    {
        public abstract Type StateType { get; }

        public abstract void DrawLeft(in FrameContext ctx);
        public abstract void DrawRight(in FrameContext ctx);

        public abstract void Enter(ComponentRuntime ctx);
        public abstract void Leave(ComponentRuntime ctx);
        public abstract void Refresh(ComponentRuntime ctx);

        public abstract DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<GlobalContext, object, TEvent> handler);

        public abstract void MakeState();
        public abstract void ClearState();
    }

    public sealed class StateObject<TState, TComponent>(
        Func<TState> factory,
        Action<ComponentRuntime, TState>? onEnter,
        Action<ComponentRuntime, TState>? onLeave,
        Action<ComponentRuntime, TState>? onRefresh) : StateObject
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        public TState State = null!;
        public TComponent Component = null!;

        public override Type StateType => typeof(TState);

        public override void Enter(ComponentRuntime ctx) => onEnter?.Invoke(ctx, State);
        public override void Leave(ComponentRuntime ctx) => onLeave?.Invoke(ctx, State);
        public override void Refresh(ComponentRuntime ctx) => onRefresh?.Invoke(ctx, State);


        public override DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<GlobalContext, object, TEvent> handler) =>
            new DeferredEvent<TState, TEvent>(evtKey, this, evt, handler);

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

    public sealed class Builder<TState, TComponent>
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        private Action<ComponentRuntime, TState>? _onEnter;
        private Action<ComponentRuntime, TState>? _onLeave;
        private Action<ComponentRuntime, TState>? _onRefresh;

        public Builder<TState, TComponent> RegisterEvent<TEvent>(EventKey eventKey,
            Action<GlobalContext, TState, TEvent> handler)
        {
            SetupDispatcher!.Register(eventKey, handler);
            return this;
        }

        public Builder<TState, TComponent> OnRefresh(Action<ComponentRuntime, TState>? handler)
        {
            _onRefresh = handler;
            return this;
        }

        public Builder<TState, TComponent> OnEnter(Action<ComponentRuntime, TState>? handler)
        {
            _onEnter = handler;
            return this;
        }

        public Builder<TState, TComponent> OnLeave(Action<ComponentRuntime, TState>? handler)
        {
            _onLeave = handler;
            return this;
        }

        public ComponentRuntime Build()
        {
            var entry = new StateObject<TState, TComponent>(() => new TState(), _onEnter, _onLeave, _onRefresh);
            var result = new ComponentRuntime(entry);
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