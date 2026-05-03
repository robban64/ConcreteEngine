using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed class VisualRenderContext
{
    public static VisualRenderContext Instance = null!;

    public static void Make(CameraRenderTransforms camera, VisualEnvironment visuals) =>
        Instance = new VisualRenderContext(camera, visuals);

    public Size2D OutputSize;
    
    public RenderFrameArgs RenderFrameArgs;
    public readonly VisualEnvironment Environment;
    public readonly CameraRenderTransforms Camera;


    private VisualRenderContext(CameraRenderTransforms camera, VisualEnvironment environment)
    {
        Camera = camera;
        Environment = environment;
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