using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Data;
using Tools.DebugInterface.Components;

namespace ConcreteEngine.Core.Interface;

internal static class DebugCommandUtils
{
    public static void RefreshShader()
    {
        
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