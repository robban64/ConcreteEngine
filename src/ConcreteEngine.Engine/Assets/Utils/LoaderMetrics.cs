using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Assets.Utils;

internal static class LoaderMetrics
{
    private static Stopwatch _loadTimer = new();
    private static long _allocStart;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Start()
    {
        _allocStart = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"Alloc Before loader: {_allocStart / 1000.0 / 1000.0}mb");
        _loadTimer.Start();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void End()
    {
        _loadTimer.Stop();
        var alloc = GC.GetAllocatedBytesForCurrentThread() - _allocStart;
        var str = $"Asset load time: {_loadTimer.ElapsedTicks / 1000.0 / 1000.0}, Alloc: {alloc / 1000.0 / 1000.0}mb\n";
        Console.Write(str);
        File.AppendAllText("diagnostic/load-time.txt", str);
        _loadTimer.Reset();
        _loadTimer = null!;
    }

}
