#region

using System.Numerics;
using ConcreteEngine.Common.Diagnostics;
using Core.DebugTools.Data;

#endregion

namespace Core.DebugTools;

public static class CoreCmdNames
{
    public const string ShaderReload = "shader-reload";
    public const string EntityTransform = "entity-transform";
}

public delegate void CommandRequestDel(DebugConsoleCtx ctx, ConsoleCmdRequest request);

public static class RouteTable
{
    private static Dictionary<string, CommandRequestDel> _commands = new(4);

    // Fetchers
    public static Func<FrameMetric<RenderInfoSample>>? PullFrameMetrics { get; set; }
    public static Func<PairSample>? PullSceneMetrics { get; set; }
    public static Func<StoreMetric<CollectionSample>>? PullMaterialMetrics { get; set; }
    public static Func<PairSample>? PullMemoryMetrics { get; set; }
    public static Action<MetricData>? FillGfxStoreMetrics { get; set; }
    public static Action<MetricData>? FillAssetMetrics { get; set; }


    internal static Dictionary<string, CommandRequestDel>.KeyCollection RegisterCommands => _commands.Keys;

    // Commands
    internal static bool InvokeCommand(DebugConsoleCtx ctx, string cmd, string? arg1, string? arg2)
    {
        if (!_commands.TryGetValue(cmd, out var handler)) return false;
        handler(ctx, new ConsoleCmdRequest(cmd, arg1, arg2));
        return true;
    }

    internal static bool InvokeCommand(DebugConsoleCtx ctx, ConsoleCmdRequest req)
    {
        if (!_commands.TryGetValue(req.Command, out var handler)) return false;
        handler(ctx, req);
        return true;
    }

    public static void RegisterCommand(string command, CommandRequestDel del) => _commands[command] = del;

    public static bool UnregisterCommand(string command) => _commands.Remove(command);

    public static void ClearCommands() => _commands.Clear();
}