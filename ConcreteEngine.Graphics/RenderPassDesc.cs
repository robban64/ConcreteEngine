using System.Drawing;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public readonly record struct RenderPassDesc(
    RenderTargetId Target,
    int Order,
    ushort FboId,
    Vector2D<int> Size,
    bool Clear,
    Color ClearColor,
    ClearBufferFlag ClearMask,
    RenderPassResolveTarget ResolveTo,
    ushort ResolveToFboId = 0
);

public readonly record struct CreateRenderPassDesc(
    RenderTargetId Target,
    int Order,
    Vector2D<int>? Size,
    bool Clear,
    Color ClearColor,
    ClearBufferFlag ClearMask,
    RenderPassResolveTarget ResolveTo,
    ushort ResolveToFboId = 0
);