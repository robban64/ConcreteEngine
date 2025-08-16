using System.Drawing;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public readonly record struct RenderTargetData(
    ushort FboId,
    ushort ColTexId,
    RenderTargetId Target,
    ushort Generation,
    Vector2D<float> SizeRatio
);

public readonly record struct RenderTargetKey(ushort Key);

public readonly record struct RenderTargetHandlerResult(ushort FboId, ushort ColTexId);

public readonly record struct NewRenderTargetDesc(
    RenderTargetId Target,
    Vector2D<float> SizeRatio
);

