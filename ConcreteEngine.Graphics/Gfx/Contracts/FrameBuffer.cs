#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public interface IFrameBufferDescriptor
{
    Vector2 DownscaleRatio { get; }
    Vector2D<int> AbsoluteSize { get; }
}

public readonly record struct FrameBufferAttachmentDesc(
    bool ColorTexture,
    bool DepthTexture,
    bool ColorBuffer,
    bool DepthStencilBuffer
);

public readonly record struct GfxFrameBufferSize(
    Vector2 DownscaleRatio, 
    Size2D AbsoluteSize, 
    bool AutoResizeable
);


public readonly record struct FrameBufferDesc(
    Vector2 DownscaleRatio,
    Vector2D<int> AbsoluteSize,
    FrameBufferAttachmentDesc Attachments,
    RenderBufferMsaa Multisample = RenderBufferMsaa.None,
    bool AutoResizeable = true,
    TexturePreset TexturePreset = TexturePreset.LinearClamp
);

public readonly record struct ColorFboDesc(
    Vector2 DownscaleRatio = default,
    Vector2D<int> AbsoluteSize = default,
    TexturePreset TexturePreset = TexturePreset.LinearClamp,
    bool DepthStencilBuffer = false
) : IFrameBufferDescriptor;

public readonly record struct DepthFboDesc(
    Vector2 DownscaleRatio = default,
    Vector2D<int> AbsoluteSize = default,
    TexturePreset TexturePreset = TexturePreset.LinearClamp,
    bool DepthStencilBuffer = false
) : IFrameBufferDescriptor;

public readonly record struct MultiSampleFboDesc(
    Vector2 DownscaleRatio = default,
    Vector2D<int> AbsoluteSize = default,
    int Samples = 4
) : IFrameBufferDescriptor;