using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Editor.Definitions;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineCommandHandler
{
    internal static EditorEngineQueue CommandQueues { get; set; }

    public static CommandResponse OnAssetShaderCmd(in EditorShaderCommand shaderCommand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shaderCommand.Name);
        switch (shaderCommand.RequestAction)
        {
            case EditorRequestAction.Reload:
                {
                    CommandQueues.EnqueueDeferred(new AssetCommandRecord(shaderCommand.Name,
                        AssetCommandAction.ReloadAsset,
                        AssetKind.Shader));
                    break;
                }
            default:
                throw new ArgumentException("Unknown RequestAction", nameof(shaderCommand.RequestAction));
        }

        return CommandResponse.Ok();
    }

    public static CommandResponse OnWorldShadowCmd(in EditorShadowCommand command)
    {
        if (command.Size <= 0)
            throw new ArgumentException("Supported shadow map size are (1024, 2048, 4096, 8192)", nameof(command.Size));

        CommandQueues.EnqueueDeferred(
            new FboCommandRecord(FboCommandAction.RecreateShadowFbo, new Size2D(command.Size)));
        return CommandResponse.Ok();
    }


    public static void OnStructSizesCmd(ConsoleCtx ctx, string action, string? arg1, string? arg2)
    {
        /*
        ctx.AddLog(StructStr<TextureSlotInfo>());
        ctx.AddLog(StructStr<Transform>());
        ctx.AddLog(StructStr<MeshComponent>());
        ctx.AddLog(StructStr<DrawCommand>());
        ctx.AddLog(StructStr<DrawCommandMeta>());
        ctx.AddLog(StructStr<MaterialUniformRecord>());
        ctx.AddLog(StructStr<DrawObjectUniform>());
        ctx.AddLog(StructStr<RecreateRequest>());
        ctx.AddLog(StructStr<GfxDebugLog>());


        ctx.AddLog(StructStr<TextureMeta>());
        ctx.AddLog(StructStr<MeshMeta>());
        ctx.AddLog(StructStr<VertexBufferMeta>());
        ctx.AddLog(StructStr<IndexBufferMeta>());
        ctx.AddLog(StructStr<FrameBufferMeta>());
        ctx.AddLog(StructStr<RenderBufferMeta>());
        ctx.AddLog(StructStr<UniformBufferMeta>());

        ctx.AddLog(StructStr<MaterialParamSnapshot>());
        ctx.AddLog(StructStr<DrawMaterialMeta>());
        ctx.AddLog(StructStr<DrawMaterialPayload>());

        ctx.AddLog(StructStr<RenderPassState>());

        ctx.AddLog(StructStr<CameraEditorPayload>());
        ctx.AddLog(StructStr<EntityDataPayload>());
        */

        ctx.AddLog(StructStr<MeshPart>());
        ctx.AddLog(StructStr<MaterialTag>());

        ctx.AddLog(StructStr<DrawEntity>());
        ctx.AddLog(StructStr<DrawEntityMeta>());
        ctx.AddLog(StructStr<DrawEntitySource>());

        ctx.AddLog(StructStr<DrawCommand>());
        ctx.AddLog(StructStr<DrawCommandMeta>());


        ctx.AddLog(StructStr<SourceComponent>());
        ctx.AddLog(StructStr<RenderAnimationComponent>());
        
        ctx.AddLog(StructStr<RenderTransform>());
    }


    private static string StructStr<T>() where T : unmanaged =>
        $"{typeof(T).Name,-18} - {Unsafe.SizeOf<T>().ToString()} bytes";
}