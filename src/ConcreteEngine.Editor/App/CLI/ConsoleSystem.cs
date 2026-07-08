using System.Diagnostics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Logging;

namespace ConcreteEngine.Editor.App.CLI;

internal sealed class ConsoleSystem
{
    public const int MaxLineLength = 256;

    public static void ExecuteCommand(Span<char> line) => _instance.Execute(line);

    private static ConsoleSystem _instance = null!;

    private readonly Dictionary<string, ConsoleCommandHandler> _commands = new(4);
    private readonly LogService _service;

    public ConsoleSystem()
    {
        _service = LogService.Instance;
        _instance = this;
    }

    public void RegisterCommand<THandler>() where THandler : ConsoleCommandHandler, new()
    {
        var handler = new THandler();
        ArgumentException.ThrowIfNullOrWhiteSpace(handler.Command);
        if (!_commands.TryAdd(handler.Command, handler))
            Throwers.InvalidArgument($"{typeof(THandler).Name} with command {handler.Command} is already registered");
    }

    public void Execute(Span<char> line)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(line.Length, MaxLineLength);
        if (line.IsEmpty || line.IsWhiteSpace()) return;
        line = line.Trim();

        {
            var cmdMsg = TextBuffers.GetWriter().Append(">> ").Append(line).EndSpan();
            _service.PushLog(cmdMsg, default, LogLevel.None, LogScope.Command);
        }

        var parts = line.Split(' ');
        var cmd = parts.MoveNext() ? line[parts.Current] : default;
        var arg1 = parts.MoveNext() ? line[parts.Current] : default;
        var arg2 = parts.MoveNext() ? line[parts.Current] : default;

        if (cmd is "clear")
        {
            _service.ClearLog();
            LogService.PushMessage("[console cleared]");
            return;
        }

        if (cmd is "help" or "info")
        {
            LogService.PushMessage("Available command handlers:");
            PrintCommandHandlers();
            return;
        }

        try
        {
            if (!_commands.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(cmd, out var command))
                Throwers.InvalidArgument(nameof(cmd));

            command.Execute(arg1, arg2);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or UnreachableException) //wip
        {
            LogService.PushMessage($"Error when invoking {cmd} with error: {ex.Message}");
        }
    }

    public void PrintCommandHandlers()
    {
        foreach (var it in _commands.Keys) LogService.PushMessage(it);
    }
}