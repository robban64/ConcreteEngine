#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderPassData
{
    public RenderPassOp Op { get; init; }
    public FrameBufferId TargetFboId { get; init; }
    public List<TextureId>? SourceTexId { get; init; }
    public FrameBufferId? BlitFboId { get; set; } = null;
    public BlendMode Blend { get; set; } = BlendMode.None;
    public bool DepthTest { get; init; } = false;
    public Vector2 SizeRatio { get; set; } = Vector2.One;
    public bool DoClear { get; init; } = false;
    public Color ClearColor { get; set; } = Color.Black;
    public ClearBufferFlag ClearMask { get; init; } = ClearBufferFlag.Color;
    public ShaderId ShaderId { get; init; } = default;

}