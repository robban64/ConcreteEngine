#region

using ConcreteEngine.Editor.Core;

#endregion

namespace ConcreteEngine.Editor.Data;

// State Delegates
internal delegate void StateTransitionDel<TModel>(ModelStateContext<TModel> ctx, TModel state) where TModel : class;

internal delegate void StateEmptyEventDel<TModel>(ModelStateContext<TModel> ctx) where TModel : class;

internal delegate void StateEventDel<TModel, in TEvent>(ModelStateContext<TModel> ctx, TEvent ev) where TModel : class;

// command delegates
public delegate void ConsoleCommandReqDel(ConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandDel<TPayload>(in TPayload payload);

public delegate CommandResponse EditorDataCommandDel<TRequest>(in TRequest request, out TRequest response)
    where TRequest : unmanaged;

// Request delegates
public delegate TResponse ApiEditorRequestDel< out TResponse>(EditorFetchHeader header) where TResponse : class;

public delegate void ApiDataRequest<TRequest>(in TRequest request, out TRequest response)
    where TRequest : unmanaged;
    