using ConcreteEngine.Editor.ViewModel;

namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandReqDel(DebugConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandReqDel<TPayload>(in TPayload payload);

// Request delegates
public delegate void GenericFillRequest<in TRequest>(TRequest  request);
public delegate bool GenericDataRequest<in TRequest, TResponse>(TRequest request, out TResponse? payload);
public delegate TResponse? GenericRequest<in TRequest, out TResponse>(TRequest request);


// Core Delegates
//internal delegate void StateTransitionDel<T>(ModelState<T>.ViewModelStateCtx ctx, T state) where T : class;

internal delegate void StateTransitionDel<T>(ModelState<T> ctx, T state) where T : class;

internal delegate void StateEventDel<T,TEvent>(ModelState<T> ctx, in TEvent ev) where T : class;

