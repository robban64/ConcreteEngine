using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Editor.Logging;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Passes;

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
        LogService.PushMessage(StructStr<DrawCommand>());
        LogService.PushMessage(StructStr<DrawCommandMeta>());
        LogService.PushMessage(StructStr<SourceComponent>());
        LogService.PushMessage(StructStr<SkinningComponent>());

        LogService.PushMessage(StructStr<GpuFrameMeta>());
        LogService.PushMessage(StructStr<GpuBufferMeta>());
        LogService.PushMessage(StructStr<FrameMetric>());
        LogService.PushMessage(StructStr<PassMutationState>());
        return;
        static string StructStr<T>() where T : struct =>
            $"{Unsafe.SizeOf<T>().ToString(),-2} {"bytes",-10} {typeof(T).Name}";
    }

}
