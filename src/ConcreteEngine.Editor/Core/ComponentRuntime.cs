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
    public void Update()
    {
        if (!Active) return;
        _stateObject.Update(_globalContext,this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateDiagnostic()
    {
        if (!Active) return;
        _stateObject.UpdateDiagnostic(_globalContext,this);
    }

    public void Enter()
    {
        if (Active) return;
        Active = true;
        _stateObject.MakeState();
        _stateObject.Enter(_globalContext,  this);
    }

    public void Leave()
    {
        if (!Active) return;
        _stateObject.Leave(_globalContext, this);
        Active = false;
    }

    public void TriggerEvent<TEvent>(EventKey eventKey, TEvent evt) =>
        _dispatcher.EnqueueEvent(eventKey, _stateObject, evt);


    internal abstract class StateObject
    {
        public abstract Type StateType { get; }

        public abstract void DrawLeft(in FrameContext ctx);
        public abstract void DrawRight(in FrameContext ctx);

        public abstract void Enter(GlobalContext ctx, ComponentRuntime component);
        public abstract void Leave(GlobalContext ctx,ComponentRuntime component);
        public abstract void Update(GlobalContext ctx,ComponentRuntime component);

        public abstract void UpdateDiagnostic(GlobalContext ctx,ComponentRuntime component);

        public abstract DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<GlobalContext, object, TEvent> handler);

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

        public override void Enter(GlobalContext ctx,ComponentRuntime component) => onEnter?.Invoke(ctx,component, State);
        public override void Leave(GlobalContext ctx,ComponentRuntime component) => onLeave?.Invoke(ctx,component, State);
        public override void Update(GlobalContext ctx,ComponentRuntime component) => onUpdate?.Invoke(ctx,component, State);
        public override void UpdateDiagnostic(GlobalContext ctx,ComponentRuntime component) => onDiagnostic?.Invoke(ctx,component, State);

        public override DeferredEvent MakeEvent<TEvent>(EventKey evtKey, TEvent evt,
            Action<GlobalContext, object, TEvent> handler) =>
            new DeferredEvent<TState, TEvent>(evtKey, State, evt, handler);

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
        private ComponentActionDel<TState>? _onEnter;
        private ComponentActionDel<TState>? _onLeave;
        private ComponentActionDel<TState>? _onUpdate;
        private ComponentActionDel<TState>? _onTickDiagnostic;

        public Builder<TState, TComponent> RegisterEvent<TEvent>(EventKey eventKey,
            Action<GlobalContext, TState, TEvent> handler)
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
            entry.Component = EditorComponent<TState>.Make<TComponent>(result);
            entry.MakeState();
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