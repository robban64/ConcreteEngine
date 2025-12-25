using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Editor.CLI;

public static class EditorCli
{
    public static readonly CliContext Context = new(AddLog);
    private static readonly ConsoleSystem Cli = new(Context);


    public static int LogCount => Cli.LogCount;
    public static int StoredLogCount => Cli.StoredLogCount;

    internal static void AddLog(string log) => Cli.Append(log);
    internal static void AddLog(StringLogEvent log) => Cli.Append(log);
    internal static void AddLogSpan(ReadOnlySpan<StringLogEvent> logs) => Cli.AppendMany(logs);

    internal static void ExecCommand(string cmd) => Cli.ExecCommand(cmd);

    internal static ConsoleSystem Service => Cli;
}