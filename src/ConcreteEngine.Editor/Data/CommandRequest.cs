using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Data;

public readonly ref struct CommandResponse(bool success, string? error)
{
    public readonly bool Success = success;
    public readonly string? Error = error;
    public static CommandResponse Ok() => new(true, null);
    public static CommandResponse Fail(string error) => new(false, error);
}

internal interface IEditorCommand
{
    EditorCommandScope Scope { get; }
}

internal sealed record ConsoleCommandRecord(string Description, bool IsNoOp, ConsoleCommandReqDel ConsoleCmdHandler);

internal sealed record EditorEditorCommand<TReq>(EditorCommandScope Scope, EditorCommandDel<TReq> EditorCmdHandler)
    : IEditorCommand;

internal sealed record EditorDataEditorCommand<TReq>(EditorCommandScope Scope, 
    EditorDataCommandDel<TReq> EditorCmdHandler) : IEditorCommand where TReq : unmanaged;