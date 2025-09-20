using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public readonly struct RenderPassFboRecord(
    FrameBufferId fboId,
    TextureId colTexId,
    RenderBufferId rboTexId,
    Vector2D<int> size,
    int samples)
{
    
    public readonly Vector2D<int> Size = size;
    public readonly FrameBufferId FboId = fboId;
    public readonly TextureId ColTexId = colTexId;
    public readonly RenderBufferId RboTexId = rboTexId;
    public readonly int Samples = samples;

    public bool Msaa => Samples > 0;
    public bool IsValid => FboId.IsValid();

}