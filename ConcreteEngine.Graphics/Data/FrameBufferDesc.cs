#region

using System.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Data;

public readonly record struct FrameBufferDesc(
    Vector2 SizeRatio,
    Vector2D<int>? AbsoluteSize = null,
    bool DepthStencilBuffer = false,
    bool Msaa = false,
    uint Samples = 0
);