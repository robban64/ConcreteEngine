using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Data;
using Tools.DebugInterface.Components;

namespace ConcreteEngine.Core.Interface;

internal static class DebugCommandController
{
    //
    internal static Action<string>? RecreateShaderAction { get; set; }
    internal static Action<int, RecreateSpecialAction>? ResizeShadowMapAction { get; set; }

    public static void OnRecreateShader(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if (RecreateShaderAction is null) return;
        if (string.IsNullOrWhiteSpace(arg1) || arg1.Length < 2) return;
        RecreateShaderAction(arg1);
        ctx.AddLog("Shader recreate enqueued");
    }

    public static void OnSetShadowMapSize(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
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

        if (size == 1024 || size == 2048 || size == 4096 || size == 8192)
        {
            ResizeShadowMapAction?.Invoke(size, RecreateSpecialAction.RecreateShadowFbo);
            return;
        }

        ctx.AddLog("Invalid argument value. Supported are (1024, 2048, 4096, 8192)");
    }

    public static void OnCmdStructSizes(DebugConsoleCtx ctx)
    {
        ctx.AddLog(GetStructStr<TextureSlotInfo>());
        ctx.AddLog(GetStructStr<Transform>());
        ctx.AddLog(GetStructStr<MeshComponent>());
        ctx.AddLog(GetStructStr<DrawCommand>());
        ctx.AddLog(GetStructStr<DrawCommandMeta>());
        ctx.AddLog(GetStructStr<MaterialUniformRecord>());
        ctx.AddLog(GetStructStr<DrawObjectUniform>());
    }

    private static string GetStructStr<T>() where T : unmanaged => $"{typeof(T).Name} - {Unsafe.SizeOf<T>()} bytes";
}