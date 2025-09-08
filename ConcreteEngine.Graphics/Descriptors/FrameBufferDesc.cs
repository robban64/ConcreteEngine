#region

using System.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public interface IFrameBufferDescriptor
{
    Vector2 DownscaleRatio { get; }
    Vector2D<int> AbsoluteSize { get; }
}

public readonly record struct ColorFboDescription(
    Vector2 DownscaleRatio,
    Vector2D<int> AbsoluteSize,
    TexturePreset TexturePreset,
    bool DepthStencilBuffer
) : IFrameBufferDescriptor;

public readonly record struct DepthFboDescription(
    Vector2 DownscaleRatio,
    Vector2D<int> AbsoluteSize,
    TexturePreset TexturePreset,
    bool DepthStencilBuffer
) : IFrameBufferDescriptor;

public readonly record struct MultiSampleFboDescription(
    Vector2 DownscaleRatio,
    Vector2D<int> AbsoluteSize,
    uint Samples
) : IFrameBufferDescriptor;

public readonly record struct FrameBufferDesc(
    Vector2 SizeRatio,
    Vector2D<int>? AbsoluteSize = null,
    bool DepthStencilBuffer = false,
    TexturePreset TexturePreset = TexturePreset.LinearClamp,
    bool Msaa = false,
    uint Samples = 0
);