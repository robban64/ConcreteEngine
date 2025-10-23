#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;
using Core.DebugTools;
using Core.DebugTools.Components;
using Core.DebugTools.Data;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

internal static class DebugController
{
    //
    private static World? _world;
    private static AssetSystem? _assetSystem;
    private static RenderEngineFrameInfo? _frameInfo;

    private static MaterialStore? Materials => _assetSystem?.MaterialStoreImpl;


    internal static void Attach(World world, AssetSystem assetSystem, RenderEngineFrameInfo frameInfo)
    {
        _world = world;
        _assetSystem = assetSystem;
        _frameInfo = frameInfo;
    }

    internal static DebugFrameMetrics GetFrameMetrics()
    {
        if (_frameInfo is null) return default;

        var gfxInfo = _frameInfo.GfxResult;
        return new DebugFrameMetrics
            (_frameInfo.FrameIndex, _frameInfo.Fps, _frameInfo.Alpha, gfxInfo.TriangleCount, gfxInfo.DrawCalls);
    }

    internal static DebugMemoryMetrics GetMemoryMetrics() => new((int)GC.GetAllocatedBytesForCurrentThread());

    internal static DebugSceneMetrics GetSceneMetrics() =>
        _world is not null ? new DebugSceneMetrics(_world.EntityCount, _world.ShadowMapSize) : default;

    internal static DebugMaterialMetrics GetMaterialMetrics() =>
        Materials is not null ? new DebugMaterialMetrics(Materials.Count, Materials.FreeSlots) : default;

    internal static void DrainAssetStoreMetrics(List<DebugAssetStoreMetricRecord> result)
    {
        if (_assetSystem is null) return;
        var store = _assetSystem.StoreImpl.GetAssetTypeMeta();
        result.Clear();
        foreach (var (k, v) in store)
            result.Add(new DebugAssetStoreMetricRecord(k.Name, v.Count, v.FileCount));
    }

    internal static void DrainGfxStoreMetrics(List<DebugGfxStoreMetricRecord> result)
    {
        var store = GfxDebugMetrics.GetStoreMetrics();
        result.Clear();
        foreach (var (k, v) in store)
            result.Add(new DebugGfxStoreMetricRecord(k.ToLogName(), v.GfxCount, v.GfxFree, v.BkCount, v.BkFree));
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
        if (string.IsNullOrWhiteSpace(arg1))
        {
            ctx.AddLog("Invalid argument");
            return;
        }

        if (!int.TryParse(arg1, out var size))
        {
            ctx.AddLog($"Invalid argument format {arg1}");
            return;
        }

        if (size == 1) size = 1024;
        if (size == 2) size = 2048;
        if (size == 4) size = 4096;
        if (size == 8) size = 8192;

        if (size == 1024 || size == 2048 || size == 4096 || size == 8192)
        {
            _assetSystem.EnqueueRecreateFrameBuffer(size, RecreateSpecialAction.RecreateShadowFbo);
            return;
        }

        ctx.AddLog("Invalid argument value. Supported are 1,2,4,8 (1024, 2048, 4096, 8192)");
    }

    public static void OnCmdStructSizes(DebugConsoleCtx ctx, string? _, string? __)
    {
        ctx.AddLog(StructStr<TextureSlotInfo>());
        ctx.AddLog(StructStr<Transform>());
        ctx.AddLog(StructStr<MeshComponent>());
        ctx.AddLog(StructStr<DrawCommand>());
        ctx.AddLog(StructStr<DrawCommandMeta>());
        ctx.AddLog(StructStr<MaterialUniformRecord>());
        ctx.AddLog(StructStr<DrawObjectUniform>());
        ctx.AddLog(StructStr<RecreateRequest>());
        ctx.AddLog(StructStr<TextureMeta>());
        ctx.AddLog(StructStr<MeshMeta>());
        ctx.AddLog(StructStr<VertexBufferMeta>());
        ctx.AddLog(StructStr<IndexBufferMeta>());
        ctx.AddLog(StructStr<FrameBufferMeta>());
        ctx.AddLog(StructStr<RenderBufferMeta>());
        ctx.AddLog(StructStr<UniformBufferMeta>());
    }

    private static string StructStr<T>() where T : unmanaged =>
        $"{typeof(T).Name,-18} - {Unsafe.SizeOf<T>().ToString()} bytes";
}