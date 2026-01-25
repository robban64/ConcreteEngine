using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.CLI;

public readonly ref struct ConsoleContext
{
    private readonly ConsoleService _service;

    internal ConsoleContext(ConsoleService service) => _service = service;

    public void LogStruct(in LogEvent log) => _service.Enqueue(log);
    public void Log(StringLogEvent log) => _service.Enqueue(log);
    public void LogPlain(string log) => _service.Enqueue(StringLogEvent.MakePlain(log));
    public void LogCommand(string log) => _service.Enqueue(StringLogEvent.MakeCommand(log));
}

public static class ConsoleGateway
{
    internal static readonly ConsoleService Service = new();

    public static ConsoleContext MakeContext() => new(Service);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogStruct(in LogEvent log) => Service.Enqueue(log);

    public static void Log(StringLogEvent? log)
    {
        ArgumentNullException.ThrowIfNull(log);
        Service.Enqueue(log);
    }

    public static void LogPlain(string? log)
    {
        ArgumentException.ThrowIfNullOrEmpty(log);
        Service.Enqueue(StringLogEvent.MakePlain(log));
    }

    internal static void ExecCommand(Span<char> cmd) => Service.ExecCommand(cmd);
}