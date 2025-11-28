#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Editor.Definitions;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EngineCommandHandler
{
    internal static EditorEngineQueue CommandQueues { get; set; } //EngineCommandRecord queue

    public static CommandResponse OnAssetShaderCmd(in EditorShaderPayload shaderPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shaderPayload.Name);
        switch (shaderPayload.RequestAction)
        {
            case EditorRequestAction.Reload:
            {
                CommandQueues.EnqueueDeferred(new AssetCommandRecord(shaderPayload.Name, AssetCommandAction.ReloadAsset,
                    AssetKind.Shader));
                break;
            }
            default:
                throw new ArgumentException("Unknown RequestAction", nameof(shaderPayload.RequestAction));
        }

        return CommandResponse.Ok();
    }

    public static CommandResponse OnWorldShadowCmd(in EditorShadowPayload payload)
    {
        if (payload.Size <= 0)
            throw new ArgumentException("Supported shadow map size are (1024, 2048, 4096, 8192)", nameof(payload.Size));

        CommandQueues.EnqueueDeferred(
            new FboCommandRecord(FboCommandAction.RecreateShadowFbo, new Size2D(payload.Size)));
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
        */

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

        ctx.AddLog(StructStr<Transform>());
        ctx.AddLog(StructStr<ModelComponent>());

        ctx.AddLog(StructStr<MeshPart>());
        ctx.AddLog(StructStr<DrawEntity>());
        ctx.AddLog(StructStr<MaterialTag>());
    }


    private static string StructStr<T>() where T : unmanaged =>
        $"{typeof(T).Name,-18} - {Unsafe.SizeOf<T>().ToString()} bytes";
}