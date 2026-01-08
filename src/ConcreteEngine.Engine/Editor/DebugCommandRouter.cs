using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Engine.Editor;

internal static class DebugCommandRouter
{
    public static void OnStructSizesCmd(ConsoleContext ctx, string action, string? arg1, string? arg2)
    {
        ctx.LogPlain(StructStr<MeshPart>());
        ctx.LogPlain(StructStr<MaterialTag>());
        ctx.LogPlain(StructStr<DrawEntity>());
        ctx.LogPlain(StructStr<DrawEntityMeta>());
        ctx.LogPlain(StructStr<DrawEntitySource>());
        ctx.LogPlain(StructStr<DrawCommand>());
        ctx.LogPlain(StructStr<DrawCommandMeta>());
        ctx.LogPlain(StructStr<SourceComponent>());
        ctx.LogPlain(StructStr<RenderAnimationComponent>());
        ctx.LogPlain(StructStr<WorldParamsData>());
        ctx.LogPlain(StructStr<EditorCameraState>());
        ctx.LogPlain(StructStr<EditorParticleState>());
        ctx.LogPlain(StructStr<EditorAnimationState>());
        ctx.LogPlain(StructStr<EditorEntityState>());

        ctx.LogPlain(StructStr<GpuFrameMetaBundle>());
        ctx.LogPlain(StructStr<GpuBufferMeta>());
        ctx.LogPlain(StructStr<PerformanceMetric>());
        ctx.LogPlain(StructStr<PassMutationState>());
        
    }


    private static string StructStr<T>() where T : unmanaged =>
        $"{Unsafe.SizeOf<T>().ToString(),-2} {"bytes",-10} {typeof(T).Name}";
}