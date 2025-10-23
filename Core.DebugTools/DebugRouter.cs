using Core.DebugTools.Components;
using Core.DebugTools.Data;

namespace Core.DebugTools;

public static class DebugRouter
{
    private static Dictionary<string, Action<DebugConsoleCtx, string?, string?>> _commands = new(4);

    // Fetchers
    public static Func<DebugFrameMetrics>? PullFrameMetrics { get; set; }
    public static Func<DebugSceneMetrics>? PullSceneMetrics { get; set; }
    public static Func<DebugMaterialMetrics>? PullMaterialMetrics { get; set; }
    public static Func<DebugMemoryMetrics>? PullMemoryMetrics { get; set; }
    public static Action<List<DebugGfxStoreMetricRecord>>? FillGfxStoreMetrics { get; set; }
    public static Action<List<DebugAssetStoreMetricRecord>>? FillAssetMetrics { get; set; }


    // Commands
    internal static bool InvokeCommand(DebugConsoleCtx ctx, string cmd, string? arg1, string? arg2)
    {
        if (!_commands.TryGetValue(cmd, out var commandHandler)) return false;
        commandHandler(ctx, arg1, arg2);
        return true;
    }

    public static void RegisterCommand(string command, Action<DebugConsoleCtx, string?, string?> commandHandler) =>
        _commands[command] = commandHandler;

    public static bool UnregisterCommand(string command) => _commands.Remove(command);

    public static void ClearCommands() => _commands.Clear();
}