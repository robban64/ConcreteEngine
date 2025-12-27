using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Specs.Visuals;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Editor;

internal static class DebugCommandRouter
{
    
    public static void OnStructSizesCmd(ConsoleContext ctx, string action, string? arg1, string? arg2)
    {
        string[] lines =
        [
            StructStr<MeshPart>(),
            StructStr<MaterialTag>(),
            StructStr<DrawEntity>(),
            StructStr<DrawEntityMeta>(),
            StructStr<DrawEntitySource>(),
            StructStr<DrawCommand>(),
            StructStr<DrawCommandMeta>(),
            StructStr<SourceComponent>(),
            StructStr<RenderAnimationComponent>(),
            StructStr<WorldParamsData>(),
            StructStr<EditorCameraState>(),
            StructStr<EditorParticleState>(),
            StructStr<EditorAnimationState>(),
            StructStr<EditorEntityState>()
        ];
        
        ctx.AddMany(lines);
    }


    private static string StructStr<T>() where T : unmanaged =>
        $"{Unsafe.SizeOf<T>().ToString(),-2} {"bytes",-10} {typeof(T).Name}";
}