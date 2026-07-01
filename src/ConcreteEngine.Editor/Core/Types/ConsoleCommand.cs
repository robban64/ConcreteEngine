using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Logging;

namespace ConcreteEngine.Editor.Core;

public delegate void ConsoleCommandDel(ConsoleContext ctx, string action, string arg1, string arg2);

public delegate TCommand ConsoleResolveDel<out TCommand>(string action, string arg1, string arg2);

public delegate CommandResponse EditorCommandDel<in TCommand>(TCommand cmd) where TCommand : EngineCommandRecord;

public readonly ref struct CommandResponse(bool success, string? error)
{
    public readonly bool Success = success;
    public readonly string? Error = error;
    public static CommandResponse Ok() => new(true, null);
    public static CommandResponse Fail(string error) => new(false, error);
}

internal sealed record ConsoleCommandMeta(string Name, string Description, bool IsNoOp);

internal sealed class ConsoleCommandEntry
{
    public required ConsoleCommandMeta Meta { get; init; }
    public required ConsoleCommandDel Handler { get; init; }
}