using System.Drawing;
using System.Numerics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public readonly record struct RenderTargetData(
    ushort FboId,
    ushort ColTexId,
    RenderTargetId Target,
    ushort Generation,
    Vector2 SizeRatio
);

public readonly record struct RenderTargetKey(ushort Key);

public readonly record struct RenderTargetHandlerResult(ushort FboId, ushort ColTexId);
