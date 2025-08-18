using System.Drawing;
using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;



public readonly record struct RenderPassData(
    RenderPassOp Op,
    FrameBufferId TargetFboId,
    TextureId? SourceTexId = null,
    FrameBufferId? BlitFboId = null, // only for blit
    BlendMode Blend = BlendMode.None,
    bool DepthTest = false,
    Vector2 SizeRatio = default,
    bool DoClear = false,
    Color ClearColor = default,
    ClearBufferFlag ClearMask = ClearBufferFlag.Color,
    ShaderId ShaderId = default
);