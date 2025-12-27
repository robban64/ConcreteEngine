using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Editor;

public static class CliName
{
    public const string AssetShader = "asset-shader";
    public const string WorldShadow = "world-shadow";
    public const string WorldParams = "world-params";
    public const string EntityTransform = "entity-transform";
    public const string CameraTransform = "camera-transform";
}
internal sealed record ConsoleCommandMeta(string Name, string Description, bool IsNoOp);

internal sealed class ConsoleCommandEntry
{
    public required ConsoleCommandMeta Meta { get; init; }
    public required ConsoleCommandDel Handler { get; init; }
}

public static class CommandDispatcher
{
    private const int DefaultCap = 16;

    private static readonly Dictionary<string, ConsoleCommandEntry> ConsoleCmd = new(DefaultCap);
    private static readonly Dictionary<string, Delegate> EditorCmd = new(DefaultCap);
    private static readonly HashSet<string> RegisteredCommands = new(DefaultCap);

    public static bool HasCommands => EditorCmd.Count > 0 || RegisteredCommands.Count > 0 || ConsoleCmd.Count > 0;

    public static void RegisterNoOpConsoleCmd(string command, string description, ConsoleCommandDel del)
    {
        var entry = new ConsoleCommandEntry
        {
            Handler = del, Meta = new ConsoleCommandMeta(command, description, true)
        };
        if (!ConsoleCmd.TryAdd(command, entry))
            throw new InvalidOperationException($"Console Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    public static void RegisterConsoleCmd<TCommand>(
        string command,
        string description,
        ConsoleResolveDel<TCommand> resolverDel
    ) where TCommand : EngineCommandRecord
    {
        if (!EditorCmd.TryGetValue(command, out var record))
            throw new InvalidOperationException($"Editor command not found for: {command}");

        if (record is not EditorCommandDel<TCommand> dispatch)
        {
            throw new InvalidOperationException(
                $"Console command require mapping to {nameof(TCommand)}, got {record.GetType().Name}");
        }

        var entry = new ConsoleCommandEntry
        {
            Handler = WrapEditorCommand(dispatch, resolverDel),
            Meta = new ConsoleCommandMeta(command, description, true)
        };

        if (!ConsoleCmd.TryAdd(command, entry))
            throw new InvalidOperationException($"Console Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    public static void RegisterEditorCmd<TCommand>(string command, EditorCommandDel<TCommand> dispatch) where TCommand : EngineCommandRecord
    {
        if (!EditorCmd.TryAdd(command, dispatch))
            throw new InvalidOperationException($"Editor Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    internal static void ProcessRegistryRecords(ConsoleContext ctx,
        Action<ConsoleContext, string, (bool, bool)> action)
    {
        foreach (var command in RegisteredCommands)
        {
            var result = (ConsoleCmd.ContainsKey(command), EditorCmd.ContainsKey(command));
            action(ctx, command, result);
        }
    }

    internal static void InvokeEditorCommand<TCommand>(string cmd, in TCommand payload)
        where TCommand : EngineCommandRecord
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cmd);

        if (!EditorCmd.TryGetValue(cmd, out var del))
            throw new KeyNotFoundException($"Unknown command: {cmd}");

        if (del is not EditorCommandDel<TCommand> dispatch)
        {
            throw new ArgumentException(
                $"Invalid payload type, expected {nameof(TCommand)}, got {del.GetType().Name}");
        }

        dispatch(payload);
    }


    // Commands
    internal static void InvokeCommand(ConsoleContext ctx, string cmd, string action, string? arg1,
        string? arg2 = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
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
                var response = editorDel(resolverDel(action, arg1, arg2));
                if (!response.Success)
                    ctx.AddLog($"Command failed: {response.Error}");
            }
            catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
            {
                ctx.AddLog($"Error executing command: {ex.Message}");
            }
        };
    }
}