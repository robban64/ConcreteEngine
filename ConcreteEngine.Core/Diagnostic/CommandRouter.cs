#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Diagnostic.utils;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;
using Core.DebugTools;
using Core.DebugTools.Components;
using Core.DebugTools.Data;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

internal static class CommandRouter
{
    //
    private static AssetSystem? _assetSystem;

    internal static void Attach(AssetSystem assetSystem)
    {
        _assetSystem = assetSystem;
    }

    public static void OnRecreateShader(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if (_assetSystem is null) return;
        if (string.IsNullOrWhiteSpace(arg1) || arg1.Length < 2) return;
        _assetSystem.EnqueueRecreateShader(arg1);
        ctx.AddLog("Shader recreate enqueued");
    }

    public static void OnSetShadowMapSize(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if (_assetSystem is null) return;
        ArgumentNullException.ThrowIfNull(arg1, nameof(arg1));
        var size = CommandUtils.IntArg(arg1);
        var shadowSize = CommandUtils.GetShadowSize(size);
        if (shadowSize <= 0)
        {
            throw new ArgumentException("Supported are 1,2,4,8 (1024, 2048, 4096, 8192)",
                nameof(arg1));
        }

        _assetSystem.EnqueueRecreateFrameBuffer(size, RecreateSpecialAction.RecreateShadowFbo);
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


    }

    private static string StructStr<T>() where T : unmanaged =>
        $"{typeof(T).Name,-18} - {Unsafe.SizeOf<T>().ToString()} bytes";
}