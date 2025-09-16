using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public readonly struct RenderPassFboRecord(
    FrameBufferId fboId,
    TextureId colTexId,
    RenderBufferId rboTexId,
    Vector2D<int> size,
    uint samples)
{
    
    public readonly Vector2D<int> Size = size;
    public readonly uint Samples = samples;
    public readonly FrameBufferId FboId = fboId;
    public readonly TextureId ColTexId = colTexId;
    public readonly RenderBufferId RboTexId = rboTexId;

    public bool Msaa => Samples > 0;
    public bool IsValid => FboId.IsValid();

    public static RenderPassFboRecord From(FrameBufferId id, in FrameBufferMeta meta, FrameBufferLayout fboLayout)
        => new(id, fboLayout.AttachedFboResources.FboTexId, fboLayout.AttachedFboResources.RboTexId, meta.Size, meta.Samples);
}