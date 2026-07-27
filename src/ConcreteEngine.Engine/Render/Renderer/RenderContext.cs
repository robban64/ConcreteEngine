using ConcreteEngine.Engine.Render.Passes;

namespace ConcreteEngine.Engine.Render.Renderer;

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