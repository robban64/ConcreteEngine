using ConcreteEngine.Engine.Render.Passes;

namespace ConcreteEngine.Engine.Render;

internal static class RenderContext
{
    public static TextureId OutputTexture;
    public static TextureId DepthTexture;
    
    public static RenderTargetKind RenderMode;
    
    public static bool IsMain => RenderMode == RenderTargetKind.Scene;
    public static bool IsDepth => RenderMode == RenderTargetKind.Shadow;
    public static void SetDepthTargetKind() => RenderMode = RenderTargetKind.Shadow;
    public static void ResetTargetKind() => RenderMode = RenderTargetKind.Scene;
}