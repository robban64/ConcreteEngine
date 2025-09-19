using System.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBufferRepository
{
    FrameBufferLayout Get(FrameBufferId fboId);
}

public readonly record struct FboAttachmentIds(
    TextureId ColorTextureId, TextureId DepthTextureId, 
    RenderBufferId ColorRenderBufferId, RenderBufferId DepthRenderBufferId
);

public sealed class FrameBufferLayout
{
    private FrameBufferDesc _desc;

    internal FrameBufferLayout(FrameBufferId fboId, in FboAttachmentIds fboAttachmentResources, in FrameBufferDesc desc)
    {
        FboId = fboId;
        FboAttachmentResources = fboAttachmentResources;
        UpdateFromDescriptor(desc);
    }

    internal void UpdateFromDescriptor(in FrameBufferDesc desc)
    {
        _desc = desc;
    }

    internal FrameBufferDesc GetDescriptor() => _desc;

    public FrameBufferId FboId { get; }
    public FboAttachmentIds FboAttachmentResources { get; }
    public RenderBufferMsaa Msaa => _desc.Multisample;
    public Vector2 DownscaleRatio => _desc.DownscaleRatio;
    public Vector2D<int> AbsoluteSize => _desc.AbsoluteSize;
    public TexturePreset TexturePreset => _desc.TexturePreset;
    public bool AutoResizeable => _desc.AutoResizeable;
}

internal sealed class FrameBufferRepository : IFrameBufferRepository
{
    private readonly Dictionary<FrameBufferId, FrameBufferLayout> _registry = new(4);

    public FrameBufferLayout Get(FrameBufferId fboId)
    {
        return _registry[fboId];
    }

    internal void AddRecord(FrameBufferId fboId, in FboAttachmentIds attachedIds, in FrameBufferDesc desc)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId.Value, nameof(fboId));
        _registry.Add(fboId, new FrameBufferLayout(fboId, in attachedIds, in desc));
    }

    internal void UpdateRecord(FrameBufferId fboId, in FrameBufferDesc desc)
    {
        _registry[fboId].UpdateFromDescriptor(in desc);
    }

    internal FrameBufferDesc GetDescriptor(FrameBufferId fboId) => _registry[fboId].GetDescriptor();
}