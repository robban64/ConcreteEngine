#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

public readonly struct RenderTargetInfo(
    FrameBufferId fboId,
    Size2D size,
    FboAttachmentIds attachments,
    RenderBufferMsaa multiSample)
{
    public FrameBufferId FboId { get; init; } = fboId;
    public Size2D Size { get; init; } = size;
    public FboAttachmentIds Attachments { get; init; } = attachments;
    public RenderBufferMsaa MultiSample { get; init; } = multiSample;
}