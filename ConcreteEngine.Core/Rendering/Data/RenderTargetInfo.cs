#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct RenderTargetInfo(
    FrameBufferId FboId,
    Size2D Size,
    FboAttachmentIds Attachments,
    RenderBufferMsaa MultiSample
);