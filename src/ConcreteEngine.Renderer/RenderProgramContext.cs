using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed class VisualRenderContext
{
    public static VisualRenderContext Instance = null!;

    public static void Make(CameraTransforms camera, VisualEnvironment visuals) =>
        Instance = new VisualRenderContext(camera, visuals);

    public float DeltaTime;
    public Size2D OutputSize;
    public TextureId OutputTexture;
    
    public readonly VisualEnvironment Environment;
    
    public readonly CameraTransforms Camera;
    
    private VisualRenderContext(CameraTransforms camera, VisualEnvironment environment)
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