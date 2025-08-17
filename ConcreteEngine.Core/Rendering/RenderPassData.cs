using System.Drawing;
using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public readonly struct RenderPassClearDesc(
    Color ClearColor = default,
    ClearBufferFlag ClearMask = ClearBufferFlag.Color
);


public readonly record struct RenderPassData(
    RenderPassOp Op,
    ushort TargetFboId,
    ushort? SourceTexId = null,
    ushort? BlitFboId = null, // only for blit
    BlendMode Blend = BlendMode.None,
    bool DepthTest = false,
    Vector2 SizeRatio = default,
    bool DoClear = false,
    Color ClearColor = default,
    ClearBufferFlag ClearMask = ClearBufferFlag.Color,
    ushort ShaderId = 0
);