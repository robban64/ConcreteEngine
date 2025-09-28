#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum RenderPassOp : byte
{
    DrawScene,
    DrawShadows,
    Blit,
    FullscreenQuad
}

public interface IRenderPassDescriptor
{
    public RenderPassOp Op { get; }
    public BlendMode Blend { get; }
    public bool DepthTest { get; }
    public GfxPassClear Clear { get; }
    public Vector2 SizeRatio { get; }
    public Vector2D<int> AbsoluteSize { get; } // Zero = full width/height
    public FrameBufferId TargetFbo { get; }
}

public interface IFsqPass : IRenderPassDescriptor
{
    public TextureId[] SourceTextures { get; }
    public ShaderId Shader { get; }
}

public interface IScenePass
{
    public GfxPassClear Clear { get; set; }
}

public interface IDepthPass
{
}

public abstract class RenderPassDescBase : IRenderPassDescriptor
{
    public abstract RenderPassOp Op { get; }
    public virtual BlendMode Blend { get; init; } = BlendMode.Unset;
    public virtual bool DepthTest { get; init; } = false;
    public virtual GfxPassClear Clear { get; set; } = GfxPassClear.MakeNoClear();

    public Vector2 SizeRatio { get; set; } = Vector2.One;
    public Vector2D<int> AbsoluteSize { get; set; } = Vector2D<int>.Zero;
    public FrameBufferId TargetFbo { get; init; }
}

public sealed class SceneRenderPass : RenderPassDescBase, IScenePass
{
    public override RenderPassOp Op => RenderPassOp.DrawScene;
    public override BlendMode Blend { get; init; } = BlendMode.Alpha;
    public override bool DepthTest { get; init; } = true;
    public override GfxPassClear Clear { get; set; } = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue);
}

public sealed class LightRenderPass : RenderPassDescBase, IScenePass
{
    public override RenderPassOp Op => RenderPassOp.DrawScene;
    public override BlendMode Blend { get; init; } = BlendMode.Additive;
    public override bool DepthTest => false;

    public override GfxPassClear Clear { get; set; } = GfxPassClear.MakeColorClear(Color4.Black);

    public new required FrameBufferId TargetFbo { get; init; }
    public required ShaderId Shader { get; init; }
}

public sealed class ShadowRenderPass : RenderPassDescBase, IDepthPass
{
    public override RenderPassOp Op => RenderPassOp.DrawShadows;
    public override bool DepthTest { get; init; } = true;

    public override GfxPassClear Clear { get; set; } = GfxPassClear.MakeDepthClear();

    public required ShaderId Shader { get; init; }
    public Vector2D<int> AtlasOffset { get; init; } = Vector2D<int>.Zero; // viewport origin within atlas
}

public sealed class BlitRenderPass : RenderPassDescBase
{
    public override RenderPassOp Op => RenderPassOp.Blit;
    public FrameBufferId BlitFbo { get; }
    public bool LinearFilter { get; set; } = true;
    public bool Multisample { get; init; } = false;
    public int Samples { get; init; } = 0;
}

public class ScreenPass : RenderPassDescBase, IFsqPass
{
    public override RenderPassOp Op => RenderPassOp.FullscreenQuad;
    public required TextureId[] SourceTextures { get; init; }
    public required ShaderId Shader { get; init; }
    public override BlendMode Blend => BlendMode.Unset;
    public override bool DepthTest => false;
    public override GfxPassClear Clear => GfxPassClear.MakeColorClear(Color4.Black);
}

public sealed class PostEffectPass : RenderPassDescBase, IFsqPass
{
    public override RenderPassOp Op => RenderPassOp.FullscreenQuad;
    public required TextureId[] SourceTextures { get; init; }
    public required TextureId OutputTexture { get; init; }
    public TextureId LutTexture { get; init; }
    public required ShaderId Shader { get; init; }
    public override GfxPassClear Clear => GfxPassClear.MakeColorClear(Color4.Black);
    public override BlendMode Blend => BlendMode.Unset;
    public override bool DepthTest => false;
    public bool GenerateMipMapAfter { get; set; } = false;
}