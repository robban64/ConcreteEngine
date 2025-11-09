using ConcreteEngine.Common;

namespace ConcreteEngine.Editor.ViewModel;

internal interface IModelState
{
    void InvokeAction(ModelStateAction action);
    void TriggerEvent<TEvent>(string eventName, TEvent eventData);
}

internal enum ModelStateAction
{
    Enter,
    Leave,
    Refresh
}
internal sealed class ModelState<T> : IModelState where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<ViewModelStateCtx, T> _onEnter;
    private readonly Action<ViewModelStateCtx, T> _onLeave;
    private readonly Action<ViewModelStateCtx, T>? _onRefresh;

    private readonly Dictionary<string, object>? _events;

    private readonly ViewModelStateCtx _ctx;

    public T? State { get; private set; }

    private ModelState(
        Func<T> factory,
        Action<ViewModelStateCtx, T> onEnter,
        Action<ViewModelStateCtx, T> onLeave,
        Action<ViewModelStateCtx, T>? onRefresh = null,
        Dictionary<string, object>? events = null)
    {
        _factory = factory;
        _onEnter = onEnter;
        _onLeave = onLeave;
        _onRefresh = onRefresh;
        _events = events;

        _ctx = new ViewModelStateCtx(this);
    }
    
    public void InvokeAction(ModelStateAction action)
    {
        switch (action)
        {
            case ModelStateAction.Enter: _onEnter(_ctx, State ??= _factory()); break;
            case ModelStateAction.Leave: _onLeave(_ctx, State!); break;
            case ModelStateAction.Refresh: _onRefresh!(_ctx, State!); break;
            default: throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    public void TriggerEvent<TEvent>(string eventName, TEvent eventData)
    {
        InvalidOpThrower.ThrowIfNull(_events, nameof(_events));
        if (!_events!.TryGetValue(eventName, out var handler))
            throw new KeyNotFoundException(eventName);

        if (handler is not EventEntry<TEvent> entry)
            throw new ArgumentException(
                $"{eventName} was invoked with invalid type: actual {typeof(TEvent).Name}, expected {nameof(entry.EventType.Name)}");

        entry.Handler(_ctx, State, eventData);
    }

    public static ViewModelStateBuilder CreateBuilder(Func<T> factory) => new(factory);

    public sealed class ViewModelStateCtx(ModelState<T> modelState)
    {
        public void CreateState()
        {
            modelState.State = modelState._factory();
        }

        public void ResetState()
        {
            modelState.State = null;
        }
    }

    public class ViewModelStateBuilder(Func<T> factory)
    {
        private Action<ViewModelStateCtx, T>? _onEnter;
        private Action<ViewModelStateCtx, T>? _onLeave;
        private Action<ViewModelStateCtx, T>? _onRefresh;
        private Dictionary<string, object>? _events;

        public ViewModelStateBuilder OnEnter(Action<ViewModelStateCtx, T> handler)
        {
            _onEnter = handler;
            return this;
        }

        public ViewModelStateBuilder OnLeave(Action<ViewModelStateCtx, T> handler)
        {
            _onLeave = handler;
            return this;
        }

        public ViewModelStateBuilder OnRefresh(Action<ViewModelStateCtx, T> handler)
        {
            _onRefresh = handler;
            return this;
        }

        public ViewModelStateBuilder RegisterEvent<TEvent>(string eventName,
            Action<ViewModelStateCtx, T, TEvent> handler)
        {
            _events ??= new Dictionary<string, object>();
            _events.Add(eventName, new EventEntry<TEvent>(handler));
            return this;
        }

        public ModelState<T> Build()
        {
            InvalidOpThrower.ThrowIfNull(factory, nameof(factory));
            InvalidOpThrower.ThrowIfNull(_onEnter, nameof(_onEnter));
            InvalidOpThrower.ThrowIfNull(_onLeave, nameof(_onLeave));
            return new ModelState<T>(factory, _onEnter!, _onLeave!, _onRefresh, _events);
        }
    }

    private sealed record EventEntry<TEvent>(Action<ViewModelStateCtx, T, TEvent> Handler)
    {
        public Type EventType { get; } = typeof(TEvent);
    }

}