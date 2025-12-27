using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Editor.Data;

// command delegates
public delegate void ConsoleCommandReqDel(ConsoleContext ctx, string action, string? arg1, string? arg2);

public delegate void CommandPayloadResolverDel<TPayload>(string action, string? arg1, string? arg2,
    out TPayload payload);

public delegate CommandResponse EditorCommandDel<TPayload>(in TPayload payload);

public delegate CommandResponse EditorDataCommandDel<TRequest>(in TRequest request, out TRequest response)
    where TRequest : unmanaged;

