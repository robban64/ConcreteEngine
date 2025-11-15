using ConcreteEngine.Editor.ViewModel;

namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandReqDel(ConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandDel<TPayload>(in TPayload payload);

public delegate CommandResponse EditorDataCommandDel<TRequest, TResponse>(in TRequest request, out TResponse response)
    where TRequest : unmanaged where TResponse : unmanaged;

// Request delegates
public delegate TResponse? GenericRequest<in TRequest, out TResponse>(TRequest request)
    where TRequest : class where TResponse : class;

// Core Delegates

internal delegate void StateTransitionDel<TModel>(ModelState<TModel> ctx, TModel state) where TModel : class;

internal delegate void StateEmptyEventDel<TModel>(ModelState<TModel> ctx) where TModel : class;
internal delegate void StateEventDel<TModel, TEvent>(ModelState<TModel> ctx, in TEvent ev) where TModel : class;