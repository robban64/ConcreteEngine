using Core.DebugTools.Components;
using Core.DebugTools.Data;

namespace Core.DebugTools;

public static class DebugRouter
{
    private static Dictionary<string, Delegate> _commands = new(4);
  
    public static void RegisterCommand(string command, Func<string?, string?, string> commandHandler) =>
        _commands[command] = commandHandler;

    public static void RegisterCommand(string command, Func<string> commandHandler) =>
        _commands[command] = commandHandler;

    public static void RegisterCommand(string command, Action<DebugConsoleCtx, string?, string?> commandHandler) =>
        _commands[command] = commandHandler;

    public static void RegisterCommand(string command, Action<DebugConsoleCtx> commandHandler) =>
        _commands[command] = commandHandler;

    public static bool UnregisterCommand(string command) => _commands.Remove(command);
    
    public static void Reset() => _commands.Clear();
    
    public static Func<DebugFrameMetrics>? PullFrameMetrics { get; set; }
    public static Func<DebugSceneMetrics>? PullSceneMetrics { get; set; }
    public static Func<DebugMaterialMetrics>? PullMaterialMetrics { get; set; }
    public static Func<DebugMemoryMetrics>? PullMemoryMetrics { get; set; }
    public static Action<List<DebugGfxStoreMetricRecord>>? FillGfxStoreMetrics { get; set; }
    public static Action<List<DebugAssetStoreMetricRecord>>? FillAssetMetrics { get; set; }

    
}