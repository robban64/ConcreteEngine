using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed class VisualRenderContext
{
    public static VisualRenderContext Instance = null!;

    public static void Make(CameraTransform camera, VisualEnvironment visuals) =>
        Instance = new VisualRenderContext(camera, visuals);

    public readonly VisualEnvironment Visuals;
    public readonly CameraTransform Camera;
    public bool UseLightSpace = false;


    public RenderFrameArgs RenderFrameArgs;

    private VisualRenderContext(CameraTransform camera, VisualEnvironment visuals)
    {
        Camera = camera;
        Visuals = visuals;
        Instance = this;
    }
}

internal sealed class RenderProgramContext
{
    public required GfxContext Gfx { get; init; }
    public required RenderRegistry Registry { get; init; }
    public required DrawCommandPipeline CommandPipeline { get; init; }
    public required RenderPassPipeline PassPipeline { get; init; }
}