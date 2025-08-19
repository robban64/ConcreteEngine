#region

using System.Numerics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Data;

public readonly record struct FrameBufferDesc(
    Vector2 SizeRatio,
    Vector2D<int>? AbsoluteSize = null,
    bool DepthStencilBuffer = false,
    TexturePreset TexturePreset = TexturePreset.LinearClamp,
    bool Msaa = false,
    uint Samples = 0
);