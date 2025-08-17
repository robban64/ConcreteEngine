using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Data;

public readonly record struct FramebufferDescriptor(
    Vector2 SizeRatio,
    Vector2D<int>? AbsoluteSize = null,
    bool DepthStencilBuffer = false,
    bool Msaa = false,
    uint Samples = 0 
);