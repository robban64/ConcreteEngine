#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Worlds.Data;
using ConcreteEngine.Core.Worlds.Entities;
using ConcreteEngine.Core.Worlds.Render;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using Core.DebugTools;
using Core.DebugTools.Components;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

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

internal abstract record CommandRequestContract(CommandRequestScope Scope, string? Arg1, string? Arg2)
{
    private static int _idx = 0;
    public int CommandId { get; } = ++_idx;
}

internal sealed record AssetCommandRequest(string Name, AssetRequestAction Action, AssetKind Kind, string? Arg1, string? Arg2)
    : CommandRequestContract(CommandRequestScope.AssetCommand, Arg1, Arg2);

internal sealed record FboCommandRequest(FboRequestAction Action, Size2D Size, string? Arg1, string? Arg2)
    : CommandRequestContract(CommandRequestScope.RenderCommand, Arg1, Arg2);

internal static class CommandRouter
{
    private static Queue<CommandRequestContract> _commandQueue = new(4);
    
    public static int CommandQueueCount => _commandQueue.Count;

    internal static void DrainCommandQueue(AssetSystem assets, WorldRenderer worldRenderer,
        Action<AssetSystem, AssetCommandRequest> onAssetDel, Action<WorldRenderer, FboCommandRequest> onRenderDel)
    {
        foreach (var command in _commandQueue)
        {
            if (command is AssetCommandRequest assetCommand) onAssetDel(assets, assetCommand);
            else if (command is FboCommandRequest renderCommand) onRenderDel(worldRenderer, renderCommand);
        }
        _commandQueue.Clear();
    }

    internal static bool TryDequeueCommand(out CommandRequestContract request)
        => _commandQueue.TryDequeue(out request);

    public static void OnRecreateShader(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if (string.IsNullOrWhiteSpace(arg1) || arg1.Length < 2)
        {
            ctx.AddMissingArg(nameof(arg1));
            return;
        }

        _commandQueue.Enqueue(new AssetCommandRequest(arg1, AssetRequestAction.ReloadAsset, AssetKind.Shader, arg1, arg2));
        ctx.AddLog("Shader recreate enqueued");
    }

    public static void OnSetShadowMapSize(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if (string.IsNullOrWhiteSpace(arg1))
        {
            ctx.AddMissingArg(nameof(arg1));
            return;
        }

        var size = CommandUtils.IntArg(arg1);
        var shadowSize = CommandUtils.GetShadowSize(size);
        if (shadowSize <= 0)
        {
            throw new ArgumentException("Supported are 1,2,4,8 (1024, 2048, 4096, 8192)",
                nameof(arg1));
        }

        _commandQueue.Enqueue(new FboCommandRequest(FboRequestAction.RecreateShadowFbo, new Size2D(shadowSize), arg1, arg2));
    }


    public static void OnCmdStructSizes(DebugConsoleCtx ctx, string? _, string? __)
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