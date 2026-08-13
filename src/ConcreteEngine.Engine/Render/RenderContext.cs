using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Render.Passes;

namespace ConcreteEngine.Engine.Render;

internal static class RenderStore
{
    public static ShaderId DepthShader;
    public static ShaderId CompositeShader;
    public static ShaderId ColorFilterShader;
    public static ShaderId PresentShader;
    public static ShaderId HighlightShader;
    public static ShaderId BoundingBoxShader;
}

internal static class RenderContext
{
    public const int ShadowSamplerSlot = 3;

    public static RenderTargetKind RenderMode;

    public static ShaderId OverrideShader;

    public static TextureId OutputTexture;
    public static TextureId DepthTexture;

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShaderId ResolveShader(ShaderId shader) => OverrideShader.IsValid() ? OverrideShader : shader;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyForDepthPass()
    {
        OverrideShader = RenderStore.DepthShader;
        RenderMode = RenderTargetKind.Shadow;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetContext()
    {
        OverrideShader = default;
        RenderMode = RenderTargetKind.Scene;
    }
    
}