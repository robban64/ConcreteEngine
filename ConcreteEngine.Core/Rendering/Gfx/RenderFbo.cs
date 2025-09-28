using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public FrameBufferId FboId { get; }
    private FrameBufferMeta _meta;

    internal RenderFbo(FrameBufferId fboId, in FrameBufferMeta meta)
    {
        FboId = fboId;
        _meta = meta;
    }

    public FboRenderData RenderData()
    {
        // ref readonly var meta = ref _getMetaDel(FboId);
        return new FboRenderData(_meta.Size, _meta.Attachments, _meta.MultiSample);
    }

    public int CompareTo(RenderFbo? other) => other!.FboId.Value.CompareTo(FboId.Value);
}

public readonly struct FboRenderData(Size2D outputSize, FboAttachmentIds attachments, RenderBufferMsaa samples)
{
    public readonly Size2D OutputSize = outputSize;
    public readonly FboAttachmentIds Attachments = attachments;
    public readonly RenderBufferMsaa Samples = samples;
}

public enum FboTagOp
{
    DrawScene,
    Multisample,
    Depth,
    Light,
    PostEffect,
    Screen
}

public interface IFboTag;

public readonly struct FboSceneTag : IFboTag;

public readonly struct FboMsaaTag : IFboTag;

public readonly struct FboPostProcessTag : IFboTag;

public readonly struct FboScreenTag : IFboTag;