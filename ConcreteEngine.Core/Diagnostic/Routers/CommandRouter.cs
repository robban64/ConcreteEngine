#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Core.Worlds.Data;
using ConcreteEngine.Core.Worlds.Entities;
using ConcreteEngine.Core.Worlds.Render;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using Core.DebugTools;
using Core.DebugTools.Data;

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

    internal static bool TryDequeueCommand(out CommandRequestContract request)
        => _commandQueue.TryDequeue(out request);

    public static void OnAssetShaderCmd(DebugConsoleCtx ctx, ConsoleCommandRequest req)
    {
        ArgumentNullException.ThrowIfNull(req.Action, nameof(req.Action));

        switch (req.Action)
        {
            case "reload":
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(req.Args, nameof(req.Args));
                _commandQueue.Enqueue(new AssetCommandRequest(req.Args, AssetRequestAction.ReloadAsset,
                    AssetKind.Shader));
                ctx.AddLog("Shader reload enqueued");
                break;
            }
            default:
                throw new ArgumentException("Unknown CommandRequestAction", nameof(req.Action));
        }
    }

    public static void OnWorldShadowCmd(DebugConsoleCtx ctx, ConsoleCommandRequest req)
    {
        var shadowSize = 0;

        if (req.Payload is GenericCmdPayload payload)
            shadowSize = payload.IntArg;

        if (req.Payload is null)
        {
            if (string.IsNullOrWhiteSpace(req.Action))
            {
                ctx.AddMissingArg(nameof(req.Action));
                return;
            }

            var size = CommandUtils.IntArg(req.Args);
            shadowSize = CommandUtils.GetShadowSize(size);
        }

        if (shadowSize <= 0)
            throw new ArgumentException("Supported are 1,2,4,8 (1024, 2048, 4096, 8192)");

        _commandQueue.Enqueue(new FboCommandRequest(FboRequestAction.RecreateShadowFbo, new Size2D(shadowSize)));
        ctx.AddLog("ShadowMap resize enqueued");
    }

    //TODO
    public static World world;

    public static void OnEntityTransformCmd(DebugConsoleCtx ctx, ConsoleCommandRequest req)
    {
        if (req.Payload is not TransformCmdPayload payload)
            throw new ArgumentException(nameof(req.Payload));

        ref var transform = ref world.Transforms.GetById(new EntityId(payload.EntityId));
        ref readonly var incomingTransform = ref payload.Transform;

        transform.Translation = incomingTransform.Translation;
        transform.Scale = incomingTransform.Scale;
        transform.Rotation = incomingTransform.Rotation;
    }


    public static void OnStructSizesCmd(DebugConsoleCtx ctx, ConsoleCommandRequest req)
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