using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.CLI;

internal sealed record ConsoleCommandMeta(string Name, string Description, bool IsNoOp);

internal sealed class ConsoleCommandEntry
{
    public required ConsoleCommandMeta Meta { get; init; }
    public required ConsoleCommandDel Handler { get; init; }
}


public static class ConsoleGateway
{
    public static readonly ConsoleContext Context = new(AddLog);
    internal static readonly ConsoleService Service = new(Context);

    public static int LogCount => Service.LogCount;
    public static int StoredLogCount => Service.StoredLogCount;

    internal static void AddLog(string log) => Service.Append(log);
    internal static void AddLog(StringLogEvent log) => Service.Append(log);
    internal static void AddLogSpan(ReadOnlySpan<StringLogEvent> logs) => Service.AppendMany(logs);

    internal static void ExecCommand(string cmd) => Service.ExecCommand(cmd);

}