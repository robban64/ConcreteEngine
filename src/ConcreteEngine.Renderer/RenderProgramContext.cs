using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

public sealed class UniformUploaderCallbacks
{
    public required Action<UniformUploadContext> UploadMainView;
    public required Action<UniformUploadContext> UploadLightView;
    public required Action<UniformUploadContext> UploadShadow;
}

internal sealed class VisualRenderContext
{
    public static VisualRenderContext Instance = null!;

    public static void Make(UniformUploaderCallbacks callbacks) =>
        Instance = new VisualRenderContext(callbacks);
    
    public Size2D OutputSize;
    public TextureId OutputTexture;
    
    public readonly Action<UniformUploadContext> UploadMainView;
    public readonly Action<UniformUploadContext> UploadLightView;
    public readonly Action<UniformUploadContext> UploadShadow;

    
    private VisualRenderContext(UniformUploaderCallbacks callbacks)
    {
        UploadMainView = callbacks.UploadMainView;
        UploadLightView = callbacks.UploadLightView;
        UploadShadow = callbacks.UploadShadow;
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