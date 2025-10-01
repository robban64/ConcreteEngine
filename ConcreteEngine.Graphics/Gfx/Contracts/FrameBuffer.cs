#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly record struct GfxFrameBufferDescriptor(
    Size2D Size,
    GfxFrameBufferDescriptor.AttachmentsDef Attachments,
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba,
    RenderBufferMsaa Multisample = RenderBufferMsaa.None,
    TexturePreset TexturePreset = TexturePreset.LinearClamp
)
{
    public readonly record struct AttachmentsDef(
        bool ColorTexture,
        bool DepthTexture,
        bool ColorBuffer,
        bool DepthStencilBuffer
    );
}