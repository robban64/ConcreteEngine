using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Renderer.Passes;

namespace ConcreteEngine.Engine.Renderer;

internal sealed class RenderContext
{
    public static RenderContext Instance = null!;
    public static void Make() => Instance = new RenderContext();

    public TextureId OutputTexture;
    public TextureId DepthTexture;
    
    public PassStateMode PassMode;


    private RenderContext()
    {
        Instance = this;
    }

    public bool IsMain => PassMode == PassStateMode.Main;
    public bool IsDepth => PassMode == PassStateMode.Depth;
    public void SetDepthMode() => PassMode = PassStateMode.Depth;
    public void ResetPassMode() => PassMode = PassStateMode.Main;
}