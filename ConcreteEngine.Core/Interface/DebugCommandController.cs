using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Data;
using Tools.DebugInterface.Components;

namespace ConcreteEngine.Core.Interface;

internal static class DebugCommandController
{
    internal static Action<string>? RecreateShaderAction { get; set; }

    public static void RecreateShader(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if(RecreateShaderAction is null) return;
        if(string.IsNullOrWhiteSpace(arg1) || arg1.Length < 2) return;
        RecreateShaderAction(arg1);
        ctx.AddLog("Shader recreate enqueued");
    }

    public static void SetShadowMapSize()
    {
        
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