using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class ComponentRuntime
{
    internal static DeferredEventDispatcher? SetupDispatcher;
    internal static StateContext? SetupContext;

    public static Builder<TComponent> CreateBuilder<TComponent>() where TComponent : EditorComponent => new();

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
        _stateObject.Update(_stateContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateDiagnostic()
    {
        if (!Active) return;
        _stateObject.UpdateDiagnostic(_stateContext);
    }

    public void Enter()
    {
        if (Active) return;
        Active = true;
        _stateObject.Enter(_stateContext);
    }

    public void Leave()
    {
        if (!Active) return;
        _stateObject.Leave(_stateContext);
        Active = false;
    }

    public void TriggerEvent<TEvent>(TEvent evt) where TEvent : ComponentEvent =>
        _dispatcher.EnqueueEvent(_stateObject, evt);


    internal abstract class StateObject
    {
        public abstract Type ComponentType { get; }
        public abstract EditorComponent Component { get; }

        public abstract void Enter(StateContext ctx);
        public abstract void Leave(StateContext ctx);
        public abstract void Update(StateContext ctx);
        public  abstract void UpdateDiagnostic(StateContext ctx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLeft(ref FrameContext ctx) => Component.DrawLeft(ref ctx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRight(ref FrameContext ctx) => Component.DrawRight(ref ctx);

        public DeferredEvent MakeEvent<TEvent>(TEvent evt, Action<StateContext, TEvent> handler)
            where TEvent : ComponentEvent
        {
            return new DeferredEvent<TEvent>(evt, handler);
        }
    }

    public sealed class StateObject<TComponent>(
        TComponent component,
        Action<StateContext, TComponent>? onEnter,
        Action<StateContext, TComponent>? onLeave,
        Action<StateContext, TComponent>? onUpdate,
        Action<StateContext, TComponent>? onDiagnostic) : StateObject
        where TComponent : EditorComponent
    {
        public override Type ComponentType => typeof(EditorComponent);
        public override TComponent Component { get; } = component;
        public override void Enter(StateContext ctx) => onEnter?.Invoke(ctx, Component);

        public override void Leave(StateContext ctx) => onLeave?.Invoke(ctx, Component);

        public override void Update(StateContext ctx) => onUpdate?.Invoke(ctx, Component);

        public override void UpdateDiagnostic(StateContext ctx) => onDiagnostic?.Invoke(ctx, Component);
    }

    public sealed class Builder<TComponent> where TComponent : EditorComponent
    {
        private Action<StateContext, TComponent>? _onEnter;
        private Action<StateContext, TComponent>? _onLeave;
        private Action<StateContext, TComponent>? _onUpdate;
        private Action<StateContext, TComponent>? _onTickDiagnostic;

        public Builder<TComponent> RegisterEvent<TEvent>(EventKey eventKey,
            Action<StateContext, TEvent> handler) where TEvent : ComponentEvent
        {
            SetupDispatcher!.Register(typeof(TEvent), eventKey, handler);
            return this;
        }

        public Builder<TComponent> OnDiagnostic(Action<StateContext, TComponent>? handler)
        {
            _onTickDiagnostic = handler;
            return this;
        }

        public Builder<TComponent> OnUpdate(Action<StateContext, TComponent>? handler)
        {
            _onUpdate = handler;
            return this;
        }

        public Builder<TComponent> OnEnter(Action<StateContext, TComponent>? handler)
        {
            _onEnter = handler;
            return this;
        }

        public Builder<TComponent> OnLeave(Action<StateContext, TComponent>? handler)
        {
            _onLeave = handler;
            return this;
        }

        public ComponentRuntime Build(TComponent component)
        {
            var entry = new StateObject<TComponent>(component, _onEnter, _onLeave, _onUpdate, _onTickDiagnostic);
            var result = new ComponentRuntime(entry);
            component.SetRuntime(result);
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