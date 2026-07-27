using ConcreteEngine.Engine.Render.Passes;

namespace ConcreteEngine.Engine.Render;

internal static class RenderContext
{
    public static TextureId OutputTexture;
    public static TextureId DepthTexture;
    
    public static PassStateMode PassMode;
    
    public static bool IsMain => PassMode == PassStateMode.Main;
    public static bool IsDepth => PassMode == PassStateMode.Depth;
    public static void SetDepthMode() => PassMode = PassStateMode.Depth;
    public static void ResetPassMode() => PassMode = PassStateMode.Main;
}