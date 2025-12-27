using System.Diagnostics;
using ConcreteEngine.Core.Diagnostics;

namespace ConcreteEngine.Editor.CLI;

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