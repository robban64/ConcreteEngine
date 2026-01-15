using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ComponentRuntime
{
    internal static DeferredEventDispatcher? SetupDispatcher;
    internal static StateContext? SetupContext;

    public static Builder<TState, TComponent> CreateBuilder<TState, TComponent>()
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        return new Builder<TState, TComponent>();
    }

    public bool Active { get; private set; }

    private readonly StateObject _stateObject;
    private readonly DeferredEventDispatcher _dispatcher;
    private readonly StateContext _stateContext;

    private ComponentRuntime(StateObject stateObject)
    {
        if (SetupContext == null || SetupDispatcher == null) throw new InvalidOperationException();
        _stateObject = stateObject;
        _dispatcher = SetupDispatcher;
        _stateContext = SetupContext;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawLeft(ref FrameContext ctx) => _stateObject.DrawLeft(ref ctx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawRight(ref FrameContext ctx) => _stateObject.DrawRight(ref ctx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (!Active) return;
        _stateObject.Update(_stateContext, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateDiagnostic()
    {
        if (!Active) return;
        _stateObject.UpdateDiagnostic(_stateContext, this);
    }

    public void Enter()
    {
        if (Active) return;
        Active = true;
        _stateObject.MakeState();
        _stateObject.Enter(_stateContext, this);
    }

    public void Leave()
    {
        if (!Active) return;
        _stateObject.Leave(_stateContext, this);
        Active = false;
    }

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent evt) =>
        _dispatcher.EnqueueEvent(eventKey, _stateObject, evt);

    public TState GetState<TState, TComponent>() where TState : class, new()
        where TComponent : EditorComponent<TState>, new()
    {
        return ((StateObject<TState, TComponent>)_stateObject).State;
    }

    internal abstract class StateObject
    {
        public abstract Type StateType { get; }

        public abstract void DrawLeft(ref FrameContext ctx);
        public abstract void DrawRight(ref FrameContext ctx);

        public abstract void Enter(StateContext ctx, ComponentRuntime component);
        public abstract void Leave(StateContext ctx, ComponentRuntime component);
        public abstract void Update(StateContext ctx, ComponentRuntime component);

        public abstract void UpdateDiagnostic(StateContext ctx, ComponentRuntime component);

        public abstract DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<StateContext, object, TEvent> handler);

        public abstract void MakeState();
        public abstract void ClearState();
    }

    public sealed class StateObject<TState, TComponent>(
        Func<TState> factory,
        ComponentActionDel<TState>? onEnter,
        ComponentActionDel<TState>? onLeave,
        ComponentActionDel<TState>? onUpdate,
        ComponentActionDel<TState>? onDiagnostic) : StateObject
        where TState : class, new() where TComponent : EditorComponent<TState>, new()
    {
        public TState State = null!;
        public TComponent Component = null!;

        public override Type StateType => typeof(TState);

        public override void Enter(StateContext ctx, ComponentRuntime component) =>
            onEnter?.Invoke(ctx, component, State);

        public override void Leave(StateContext ctx, ComponentRuntime component) =>
            onLeave?.Invoke(ctx, component, State);

        public override void Update(StateContext ctx, ComponentRuntime component) =>
            onUpdate?.Invoke(ctx, component, State);

        public override void UpdateDiagnostic(StateContext ctx, ComponentRuntime component) =>
            onDiagnostic?.Invoke(ctx, component, State);

        public override DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<StateContext, object, TEvent> handler) =>
            new DeferredEvent<TState, TEvent>(evtKey, State, evt, handler);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void DrawLeft(ref FrameContext ctx) => Component.DrawLeft(State, ref ctx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void DrawRight(ref FrameContext ctx) => Component.DrawRight(State, ref ctx);

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
        private ComponentActionDel<TState>? _onEnter;
        private ComponentActionDel<TState>? _onLeave;
        private ComponentActionDel<TState>? _onUpdate;
        private ComponentActionDel<TState>? _onTickDiagnostic;

        public Builder<TState, TComponent> RegisterEvent<TEvent>(EventKey eventKey,
            Action<StateContext, TState, TEvent> handler)
        {
            SetupDispatcher!.Register(eventKey, handler);
            return this;
        }

        public Builder<TState, TComponent> OnDiagnostic(ComponentActionDel<TState>? handler)
        {
            _onTickDiagnostic = handler;
            return this;
        }

        public Builder<TState, TComponent> OnUpdate(ComponentActionDel<TState>? handler)
        {
            _onUpdate = handler;
            return this;
        }

        public Builder<TState, TComponent> OnEnter(ComponentActionDel<TState>? handler)
        {
            _onEnter = handler;
            return this;
        }

        public Builder<TState, TComponent> OnLeave(ComponentActionDel<TState>? handler)
        {
            _onLeave = handler;
            return this;
        }

        public ComponentRuntime Build()
        {
            var entry = new StateObject<TState, TComponent>(() => new TState(), _onEnter, _onLeave, _onUpdate,
                _onTickDiagnostic);
            var result = new ComponentRuntime(entry);
            entry.MakeState();
            entry.Component = EditorComponent<TState>.Make<TComponent>(result, entry.State);
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