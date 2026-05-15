using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

public sealed unsafe class UniformUploaderCallbacks
{
    public required delegate*<in UniformUploadContext, void> UploadMainView;
    public required delegate*<in UniformUploadContext, void> UploadLightView;
    public required delegate*<in UniformUploadContext, void> UploadShadow;
}

internal sealed class VisualRenderContext
{
    public static VisualRenderContext Instance = null!;

    public static void Make(UniformUploaderCallbacks callbacks) => Instance = new VisualRenderContext(callbacks);

    public Size2D OutputSize;
    public TextureId OutputTexture;

    public readonly UniformUploaderCallbacks UniformCallbacks;


    private VisualRenderContext(UniformUploaderCallbacks callbacks)
    {
        UniformCallbacks = callbacks;
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