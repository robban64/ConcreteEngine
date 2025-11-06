using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Data;

public delegate void ConsoleCommandReqDel(DebugConsoleCtx ctx, string action, string? arg1, string? arg2);
public delegate void PayloadResolver<TPayload>(string action, string? arg1, string? arg2, out TPayload payload);
public delegate CommandResponse EditorCommandReqDel<TPayload>(in TPayload payload);

public readonly ref struct CommandResponse(bool success, string? error)
{
    public readonly bool Success  = success;
    public readonly string? Error  = error;
    public static CommandResponse Ok() => new(true, null);
    public static CommandResponse Fail(string error) => new(false, error);
}



internal sealed record ConsoleCommandRecord(string Description, bool IsNoOp, ConsoleCommandReqDel ConsoleCmdHandler);
internal sealed record EditorCommandRecord(EditorCommandScope Scope, Type PayloadType, Delegate EditorCmdHandler);
