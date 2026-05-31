using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

public sealed unsafe class UniformUploaderCallbacks
{
    public required delegate*<in UniformUploadContext, void> UploadMainView;
    public required delegate*<in UniformUploadContext, void> UploadLightView;
    public required delegate*<in UniformUploadContext, void> UploadShadow;
}

internal sealed class RenderContext
{
    public static RenderContext Instance = null!;
    public static void Make(UniformUploaderCallbacks callbacks) => Instance = new RenderContext(callbacks);

    public Size2D OutputSize;
    public TextureId OutputTexture;
    public PassStateMode PassMode { get; private set; }

    public readonly UniformUploaderCallbacks UniformCallbacks;

    private RenderContext(UniformUploaderCallbacks callbacks)
    {
        UniformCallbacks = callbacks;
        Instance = this;
    }

    public bool IsMain => PassMode == PassStateMode.Main;
    public bool IsDepth => PassMode == PassStateMode.Depth;
    public void SetDepthMode() => PassMode = PassStateMode.Depth;
    public void ResetPassMode() => PassMode = PassStateMode.Main;
}