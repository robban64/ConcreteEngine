using ConcreteEngine.Editor.ViewModel;

namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandReqDel(DebugConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandReqDel<TPayload>(in TPayload payload);

// Request delegates
public delegate void GenericDataRequest<TData>(ref TData data) where TData : unmanaged;
public delegate TResponse? GenericRequest<in TRequest, out TResponse>(TRequest request)
    where TRequest : class where TResponse : class;

// Core Delegates

internal delegate void StateTransitionDel<T>(ModelState<T> ctx, T state) where T : class;

internal delegate void StateEventDel<T,TEvent>(ModelState<T> ctx, in TEvent ev) where T : class;

