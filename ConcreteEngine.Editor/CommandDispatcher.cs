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
    public const string WorldParams = "world-params";
    public const string EntityTransform = "entity-transform";
    public const string CameraTransform = "camera-transform";
}

public static class CommandDispatcher
{
    private static readonly Dictionary<string, ConsoleCommandRecord> ConsoleCmd = new(8);
    private static readonly Dictionary<string, IEditorCommand> EditorCmd = new(8);
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
        if (!EditorCmd.TryGetValue(command, out var record))
            throw new InvalidOperationException($"Editor command not found for: {command}");

        if (record is not EditorEditorCommand<TPayload> editorCmd)
        {
            throw new InvalidOperationException(
                $"Console command require mapping to {nameof(EditorEditorCommand<TPayload>)}, got {record.GetType().Name}");
        }

        var del = WrapEditorCommand(editorCmd.EditorCmdHandler, resolverDel);
        if (!ConsoleCmd.TryAdd(command, new ConsoleCommandRecord(description, false, del)))
            throw new InvalidOperationException($"Console Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    public static void RegisterEditorCmd<TPayload>(string command, EditorCommandScope scope,
        EditorCommandDel<TPayload> handler)
    {
        if (!EditorCmd.TryAdd(command, new EditorEditorCommand<TPayload>(scope, handler)))
            throw new InvalidOperationException($"Editor Command {command} is already registered");

        RegisteredCommands.Add(command);
    }

    public static void RegisterEditorDataCmd<TReq, TRes>(string command, EditorCommandScope scope,
        EditorDataCommandDel<TReq, TRes> handler) where TReq : unmanaged where TRes : unmanaged
    {
        if (!EditorCmd.TryAdd(command, new EditorDataEditorCommand<TReq, TRes>(scope, handler)))
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

    internal static void ExecuteDataCommand<TReq, TRes>(string cmd, in TReq request, out TRes response)
        where TReq : unmanaged where TRes : unmanaged
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cmd, nameof(cmd));

        if (!EditorCmd.TryGetValue(cmd, out var record))
            throw new KeyNotFoundException($"Unknown command: {cmd}");

        if (record is not EditorDataEditorCommand<TReq, TRes> tRecord)
        {
            var name = typeof(EditorDataEditorCommand<TReq, TRes>).Name;
            throw new ArgumentException($"Invalid payload type, expected {name}, got {record.GetType().Name}");
        }

        tRecord.EditorCmdHandler(in request, out response);
    }

    internal static void InvokeEditorCommand<TPayload>(string cmd, in TPayload payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cmd, nameof(cmd));

        if (!EditorCmd.TryGetValue(cmd, out var record))
            throw new KeyNotFoundException($"Unknown command: {cmd}");

        if (record is not EditorEditorCommand<TPayload> tRecord)
        {
            throw new ArgumentException(
                $"Invalid payload type, expected {nameof(TPayload)}, got {record.GetType().Name}");
        }

        tRecord.EditorCmdHandler(in payload);
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
    private static ConsoleCommandReqDel WrapEditorCommand<TReq>(EditorCommandDel<TReq> editorDel,
        CommandPayloadResolverDel<TReq> resolverDel)
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