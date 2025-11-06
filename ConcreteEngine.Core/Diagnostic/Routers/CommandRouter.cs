#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Core.Worlds.Data;
using ConcreteEngine.Core.Worlds.Entities;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Diagnostic.Routers;

internal enum CommandRequestScope : byte
{
    None = 0,
    CoreCommand = 1,
    WorldCommand = 2,
    AssetCommand = 3,
    RenderCommand = 4
}

internal enum AssetRequestAction : byte
{
    None = 0,
    ReloadAsset
}

internal enum FboRequestAction : byte
{
    None = 0,
    RecreateScreenDependentFbo = 1,
    RecreateShadowFbo = 2,
}

internal abstract record CommandRequestContract(CommandRequestScope Scope)
{
    private static int _idx = 0;
    public int CommandId { get; } = ++_idx;
}

internal sealed record AssetCommandRequest(string Name, AssetRequestAction Action, AssetKind Kind)
    : CommandRequestContract(CommandRequestScope.AssetCommand);

internal sealed record FboCommandRequest(FboRequestAction Action, Size2D Size)
    : CommandRequestContract(CommandRequestScope.RenderCommand);

internal static class CommandRouter
{
    private static Queue<CommandRequestContract> _commandQueue = new(4);

    public static int CommandQueueCount => _commandQueue.Count;

    internal static bool TryDequeueCommand(out CommandRequestContract request) => _commandQueue.TryDequeue(out request);

    public static CommandResponse OnAssetShaderCmd(in EditorShaderPayload shaderPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shaderPayload.Name);
        switch (shaderPayload.RequestAction)
        {
            case EditorRequestAction.Reload:
            {
                var result = new AssetCommandRequest(shaderPayload.Name, AssetRequestAction.ReloadAsset,
                    AssetKind.Shader);
                _commandQueue.Enqueue(result);
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

        _commandQueue.Enqueue(new FboCommandRequest(FboRequestAction.RecreateShadowFbo, new Size2D(payload.Size)));
        return CommandResponse.Ok();
    }

    //TODO
    public static World world;

    public static CommandResponse OnEntityTransformCmd(in EditorTransformPayload payload)
    {
        ref var entityTransform = ref world.Transforms.GetById(new EntityId(payload.EntityId));
        ref readonly var transform = ref payload.Transform;

        entityTransform.Translation = transform.Translation;
        entityTransform.Scale = transform.Scale;
        entityTransform.Rotation = transform.Rotation;
        return CommandResponse.Ok();
    }


    public static void OnStructSizesCmd(DebugConsoleCtx ctx, string action, string? arg1, string? arg2)
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

        ctx.AddLog(StructStr<StoreMetric<CollectionSample>>());
        ctx.AddLog(StructStr<GfxResourceMetric<ValueSample>>());

        ctx.AddLog(StructStr<GfxStoreMetricsPayload>());
        ctx.AddLog(StructStr<MaterialParams>());
        ctx.AddLog(StructStr<DrawMaterialMeta>());
        ctx.AddLog(StructStr<DrawMaterialPayload>());

        ctx.AddLog(StructStr<GfxPassState>());

        ctx.AddLog(StructStr<Transform>());

        ctx.AddLog(StructStr<MeshPart>());
        ctx.AddLog(StructStr<DrawEntity>());
        ctx.AddLog(StructStr<MaterialTag>());

    }


    private static string StructStr<T>() where T : unmanaged =>
        $"{typeof(T).Name,-18} - {Unsafe.SizeOf<T>().ToString()} bytes";
}