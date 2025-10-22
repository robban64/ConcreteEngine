using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using Tools.DebugInterface.Components;

namespace ConcreteEngine.Core.Diagnostic;

internal static class DebugCommandController
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

    public static void OnCmdStructSizes(DebugConsoleCtx ctx)
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

    private static string StructStr<T>() where T : unmanaged 
        => $"{typeof(T).Name,-18} - {Unsafe.SizeOf<T>().ToString()} bytes";
}