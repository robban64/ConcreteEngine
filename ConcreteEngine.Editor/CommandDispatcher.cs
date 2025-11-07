#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;

#endregion

namespace ConcreteEngine.Editor;

public static class CoreCmdNames
{
    public const string AssetShader = "asset-shader";
    public const string WorldShadow = "world-shadow";
    public const string EntityTransform = "entity-transform";
    public const string CameraTransform = "camera-transform";
}

public static class CommandDispatcher
{
    private static readonly Dictionary<string, ConsoleCommandRecord> ConsoleCmd = new(8);
    private static readonly Dictionary<string, EditorCommandRecord> EditorCmd = new(8);
    private static readonly HashSet<string> RegisteredCommands = new(8);


    public static void RegisterNoOpConsoleCmd(string command, string description, ConsoleCommandReqDel del)
    {
        if (!ConsoleCmd.TryAdd(command, new ConsoleCommandRecord(description, true, del)))
            throw new InvalidOperationException($"Console Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    public static void RegisterConsoleCmd<TPayload>(string command, string description,
        CommandPayloadResolverDel<TPayload> resolverDel)
    {
        var editorDel = (EditorCommandReqDel<TPayload>)EditorCmd[command].EditorCmdHandler;
        var del = WrapEditorCommand(editorDel, resolverDel);
        if (!ConsoleCmd.TryAdd(command, new ConsoleCommandRecord(description, false, del)))
            throw new InvalidOperationException($"Console Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    public static void RegisterEditorCmd<TPayload>(string command, EditorCommandScope scope,
        EditorCommandReqDel<TPayload> handler)
    {
        if (!EditorCmd.TryAdd(command, new EditorCommandRecord(scope, typeof(TPayload), handler)))
            throw new InvalidOperationException($"Editor Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    internal static void ProcessRegistryRecords(DebugConsoleCtx ctx,
        Action<DebugConsoleCtx, string, (bool, bool)> action)
    {
        foreach (var command in RegisteredCommands)
        {
            var result = (ConsoleCmd.ContainsKey(command), EditorCmd.ContainsKey(command));
            action(ctx, command, result);
        }
    }

    internal static void InvokeEditorCommand<TPayload>(string cmd, in TPayload payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cmd, nameof(cmd));

        if (!EditorCmd.TryGetValue(cmd, out var record))
            throw new KeyNotFoundException($"Unknown command: {cmd}");

        if (typeof(TPayload) != record.PayloadType)
        {
            throw new ArgumentException(
                $"Invalid payload type, expected {record.PayloadType.Name}, got {typeof(TPayload).Name}");
        }

        ((EditorCommandReqDel<TPayload>)record.EditorCmdHandler)(in payload);
    }

    // Commands
    internal static void InvokeCommand(DebugConsoleCtx ctx, string cmd, string action, string? arg1,
        string? arg2 = null)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentException.ThrowIfNullOrWhiteSpace(cmd, nameof(cmd));

        if (!ConsoleCmd.TryGetValue(cmd, out var record))
            throw new KeyNotFoundException($"Unknown command: {cmd}");

        record.ConsoleCmdHandler(ctx, action, arg1, arg2);
    }


    // create closure over command
    private static ConsoleCommandReqDel WrapEditorCommand<TPayload>(EditorCommandReqDel<TPayload> editorDel,
        CommandPayloadResolverDel<TPayload> resolverDel)
    {
        return (ctx, action, arg1, arg2) =>
        {
            try
            {
                resolverDel(action, arg1, arg2, out var payload);
                var response = editorDel(in payload);
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