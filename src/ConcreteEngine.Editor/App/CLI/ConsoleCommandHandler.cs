using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Editor.Logging;

namespace ConcreteEngine.Editor.App.CLI;

internal abstract class ConsoleCommandHandler(string command)
{
    public string Command { get; } = command;
    public abstract void Execute(ReadOnlySpan<char> arg1, ReadOnlySpan<char> arg2);
}

internal sealed class AssetCommandHandler() : ConsoleCommandHandler("asset")
{
    public override void Execute(ReadOnlySpan<char> arg1, ReadOnlySpan<char> arg2) { }
}

internal sealed class UtilityCommandHandler() : ConsoleCommandHandler("utility")
{
    public override void Execute(ReadOnlySpan<char> arg1, ReadOnlySpan<char> arg2)
    {
        if (arg1 is "struct-size") OnStructSizesCmd();
    }

    private static void OnStructSizesCmd()
    {
        LogService.PushMessage(StructStr<RenderSource>());
        LogService.PushMessage(StructStr<SkinningLink>());
        LogService.PushMessage(StructStr<GpuBufferMeta>());
        LogService.PushMessage(StructStr<FrameMetric>());
        return;

        static string StructStr<T>() where T : struct =>
            $"{Unsafe.SizeOf<T>().ToString(),-2} {"bytes",-10} {typeof(T).Name}";
    }
}