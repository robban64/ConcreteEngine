using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer;

internal sealed class VisualRenderContext
{
    public static VisualRenderContext Instance = null!;
    public static void Make(CameraTransform camera) => Instance = new VisualRenderContext(camera);
    
    public readonly CameraTransform Camera;
    public bool UseLightSpace = false;

    private VisualRenderContext(CameraTransform camera)
    {
        Camera = camera;
        Instance = this;
    }
}
internal sealed class RenderStateContext
{
    public readonly RenderParamsSnapshot Snapshot = new();
    public required CameraTransform Camera;

    public bool UseLightSpace = false;

    public RenderFrameArgs RenderFrameArgs;
    public MeshId FsqMesh;
}

internal sealed class RenderProgramContext
{
    public required GfxContext Gfx { get; init; }
    public required RenderRegistry Registry { get; init; }
    public required DrawCommandPipeline CommandPipeline { get; init; }
    public required RenderPassPipeline PassPipeline { get; init; }
}