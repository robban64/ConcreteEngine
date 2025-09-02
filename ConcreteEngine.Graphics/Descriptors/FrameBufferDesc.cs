#region

using System.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public readonly record struct FrameBufferDesc(
    Vector2 SizeRatio,
    Vector2D<int>? AbsoluteSize = null,
    bool DepthStencilBuffer = false,
    TexturePreset TexturePreset = TexturePreset.LinearClamp,
    bool Msaa = false,
    uint Samples = 0
);