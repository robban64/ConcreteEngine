using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Renderer.Data;

public readonly struct RenderTargetInfo(
    FrameBufferId fboId,
    Size2D size,
    FboAttachmentIds attachments,
    RenderBufferMsaa multiSample)
{
    public readonly FrameBufferId FboId = fboId;
    public readonly Size2D Size = size;
    public readonly FboAttachmentIds Attachments = attachments;
    public readonly RenderBufferMsaa MultiSample = multiSample;
}