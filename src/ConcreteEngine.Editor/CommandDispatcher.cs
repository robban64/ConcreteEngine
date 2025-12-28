using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Editor;

public static class CliName
{
    public const string Asset = "asset";
    public const string Graphics = "graphics";
}

public static class CommandDispatcher
{
    private const int DefaultCap = 16;

    private static readonly Dictionary<string, ConsoleCommandEntry> ConsoleCmd = new(DefaultCap);
    private static readonly Dictionary<Type, Delegate> EditorCmd = new(DefaultCap);

    public static void RegisterNoOpConsoleCmd(string command, string description, ConsoleCommandDel del)
    {
        var entry = new ConsoleCommandEntry
        {
            Handler = del, Meta = new ConsoleCommandMeta(command, description, true)
        };
        if (!ConsoleCmd.TryAdd(command, entry))
            throw new InvalidOperationException($"Console Command {command} is already registered");
    }

    public static void RegisterConsoleCmd<TCommand>(
        string command,
        string description,
        ConsoleResolveDel<TCommand> resolverDel
    ) where TCommand : EngineCommandRecord
    {
        if (!EditorCmd.TryGetValue(typeof(TCommand), out var record))
            throw new InvalidOperationException($"Editor command not found for: {typeof(TCommand).Name}");

        if (record is not EditorCommandDel<TCommand> dispatch)
        {
            throw new InvalidOperationException(
                $"Expected Command {typeof(TCommand).Name}, got {record.GetType().Name}");
        }

        var entry = new ConsoleCommandEntry
        {
            Handler = WrapEditorCommand(dispatch, resolverDel),
            Meta = new ConsoleCommandMeta(command, description, false)
        };

        if (!ConsoleCmd.TryAdd(command, entry))
            throw new InvalidOperationException($"Console Command {command} is already registered");
    }

    public static void RegisterCommand<TCommand>(EditorCommandDel<TCommand> dispatch)
        where TCommand : EngineCommandRecord
    {
        if (!EditorCmd.TryAdd(typeof(TCommand), dispatch))
            throw new InvalidOperationException($"Editor Command {typeof(TCommand).Name} is already registered");
    }

    internal static void ProcessCommandEntries(ConsoleContext ctx, Action<ConsoleContext, ConsoleCommandMeta> action)
    {
        foreach (var it in ConsoleCmd.Values)
            action(ctx, it.Meta);
    }

    internal static void InvokeEditorCommand<TCommand>(TCommand cmd) where TCommand : EngineCommandRecord
    {
        ArgumentNullException.ThrowIfNull(cmd);

        if (!EditorCmd.TryGetValue(typeof(TCommand), out var del))
            throw new KeyNotFoundException($"Unknown command: {cmd}");

        if (del is not EditorCommandDel<TCommand> dispatch)
            throw new ArgumentException($"Invalid command expected {typeof(TCommand).Name}, got {del.GetType().Name}");

        try
        {
            var response = dispatch(cmd, new EngineCommandMeta());
            if (!response.Success)
                ConsoleGateway.AddLog($"Command failed: {response.Error}");
        }
        catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
        {
            ConsoleGateway.AddLog($"Error executing command: {ex.Message}");
        }
    }


    // Commands
    internal static void InvokeCommand(ConsoleContext ctx, string cmd, string action, string? arg1, string? arg2 = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cmd);

        if (!ConsoleCmd.TryGetValue(cmd, out var entry))
            throw new KeyNotFoundException($"Unknown command: {cmd}");

        entry.Handler(ctx, action, arg1, arg2);
    }


    // create closure over command
    private static ConsoleCommandDel WrapEditorCommand<TCommand>(
        EditorCommandDel<TCommand> editorDel,
        ConsoleResolveDel<TCommand> resolverDel) where TCommand : EngineCommandRecord
    {
        return (ctx, action, arg1, arg2) =>
        {
            try
            {
                var response = editorDel(resolverDel(action, arg1, arg2), new EngineCommandMeta());
                if (!response.Success)
                    ctx.AddLog($"Cli command failed: {response.Error}");
            }
            catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
            {
                ctx.AddLog($"Error executing cli command: {ex.Message}");
            }
        };
    }
}