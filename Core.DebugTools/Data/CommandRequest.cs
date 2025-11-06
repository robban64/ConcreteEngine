using System.Numerics;

namespace Core.DebugTools.Data;

public delegate void ConsoleCommandReqDel(DebugConsoleCtx ctx, string action, string? arg1, string? arg2);
public delegate void PayloadResolver<TPayload>(string action, string? arg1, string? arg2, out TPayload payload);
public delegate CommandResponse EditorCommandReqDel<TPayload>(in TPayload payload);

public struct EmptyPayload {}

public readonly ref struct CommandResponse(bool success, string? error)
{
    public readonly bool Success  = success;
    public readonly string? Error  = error;
    public static CommandResponse Ok() => new(true, null);
    public static CommandResponse Fail(string error) => new(false, error);
}

public enum ConsoleCommandScope
{
    None,
    Engine,
    Editor,
    Diagnostic
}
internal sealed record ConsoleCommandRecord(string Description, bool IsNoOp, ConsoleCommandReqDel ConsoleCmdHandler);
internal sealed record EditorCommandRecord(ConsoleCommandScope Scope, Type PayloadType, Delegate EditorCmdHandler);


//

public sealed record ConsoleCommandRequest(
    string Command,
    string? Action = null,
    string? Args = null,
    IConsoleCmdPayload? Payload = null
);

public interface IConsoleCmdPayload;

public sealed record GenericCmdPayload(
    int TargetId,
    int IntArg = 0,
    float FloatArg = 0
) : IConsoleCmdPayload;

public sealed class TransformCmdPayload(int entityId, in EntityEditorTransform transform) : IConsoleCmdPayload
{
    public int EntityId { get; init; } = entityId;

    private readonly EntityEditorTransform _transform = transform;
    public ref readonly EntityEditorTransform Transform => ref _transform;
}