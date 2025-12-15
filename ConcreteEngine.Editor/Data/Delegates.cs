namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandReqDel(ConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandDel<TPayload>(in TPayload payload);

public delegate CommandResponse EditorDataCommandDel<TRequest>(in TRequest request, out TRequest response)
    where TRequest : unmanaged;

// Request delegates
public delegate TResponse ApiEditorRequestDel<out TResponse>(EditorFetchHeader header) where TResponse : class;