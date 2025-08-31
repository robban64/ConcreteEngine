#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum RenderPassOp : byte
{
    DrawScene,
    Blit,
    FullscreenQuad
}

public enum RenderTargetId : byte
{
    Scene,
    SceneLight,
    Screen
}

public record struct RenderPassClearDesc(Color4 ClearColor, ClearBufferFlag ClearMask);

public interface IRenderPass
{
    public RenderPassOp Op { get; }
    public BlendMode Blend { get; }
    public bool DepthTest { get; }
    public RenderPassClearDesc? Clear { get; }
    public Vector2 SizeRatio { get; }
    public FrameBufferId TargetFbo { get; }
}

public sealed class SceneRenderPass : IRenderPass
{
    public RenderPassOp Op => RenderPassOp.DrawScene;
    public BlendMode Blend { get; set; } = BlendMode.Alpha;
    public Vector2 SizeRatio { get; set; } = Vector2.One;
    public bool DepthTest { get; set; } = true;

    public RenderPassClearDesc? Clear { get; set; } =
        new RenderPassClearDesc(Colors.Black, ClearBufferFlag.ColorAndDepth);

    public required FrameBufferId TargetFbo { get; set; }
}

public sealed class LightRenderPass : IRenderPass
{
    public RenderPassOp Op => RenderPassOp.DrawScene;
    public BlendMode Blend { get; set; } = BlendMode.Additive;
    public Vector2 SizeRatio { get; set; } = Vector2.One;
    public bool DepthTest { get; set; } = false;
    public RenderPassClearDesc? Clear { get; set; } = new RenderPassClearDesc(Colors.Black, ClearBufferFlag.Color);

    public required FrameBufferId TargetFbo { get; set; }
    public required ShaderId Shader { get; set; }
}

public sealed class BlitRenderPass : IRenderPass
{
    public RenderPassOp Op => RenderPassOp.Blit;
    public BlendMode Blend => BlendMode.None;
    public bool DepthTest => false;
    public RenderPassClearDesc? Clear => null;

    public Vector2 SizeRatio { get; set; } = Vector2.One;
    public bool LinearFilter { get; set; } = true;
    public required FrameBufferId TargetFbo { get; set; }
    public required FrameBufferId BlitFbo { get; set; }
    public bool Multisample { get; set; } = false;
    public int Samples { get; init; } = 0;
}

public sealed class FsqRenderPass : IRenderPass
{
    public RenderPassOp Op => RenderPassOp.FullscreenQuad;
    public BlendMode Blend => BlendMode.None;
    public bool DepthTest => false;
    public RenderPassClearDesc? Clear => null;
    public Vector2 SizeRatio { get; set; } = Vector2.One;
    public FrameBufferId TargetFbo { get; set; }
    public required TextureId[] SourceTextures { get; set; }
    public required ShaderId Shader { get; set; }
}