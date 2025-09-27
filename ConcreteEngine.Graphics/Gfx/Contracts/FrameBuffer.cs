#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

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

