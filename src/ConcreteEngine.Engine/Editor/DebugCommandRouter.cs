using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Engine.Editor;

internal static class DebugCommandRouter
{
    public static void OnStructSizesCmd(ConsoleContext ctx, string action, string? arg1, string? arg2)
    {
        ctx.LogPlain(StructStr<DrawEntity>());
        ctx.LogPlain(StructStr<DrawEntityMeta>());
        ctx.LogPlain(StructStr<DrawEntitySource>());
        ctx.LogPlain(StructStr<DrawCommand>());
        ctx.LogPlain(StructStr<DrawCommandMeta>());
        ctx.LogPlain(StructStr<SourceComponent>());
        ctx.LogPlain(StructStr<RenderAnimationComponent>());

        ctx.LogPlain(StructStr<GpuFrameMeta>());
        ctx.LogPlain(StructStr<GpuBufferMeta>());
        ctx.LogPlain(StructStr<FrameMetric>());
        ctx.LogPlain(StructStr<PassMutationState>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string StructStr<T>() where T : unmanaged =>
        $"{Unsafe.SizeOf<T>().ToString(),-2} {"bytes",-10} {typeof(T).Name}";
}