namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandReqDel(DebugConsoleCtx ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandReqDel<TPayload>(in TPayload payload);

// editor view delegates
public delegate void GenericFillRequest<in TRequest>(TRequest  request);
public delegate bool GenericDataRequest<in TRequest, TResponse>(TRequest request, out TResponse? payload);
public delegate TResponse? GenericRequest<in TRequest, out TResponse>(TRequest request);
