using System.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBufferRepository
{
    FrameBufferLayout Get(FrameBufferId fboId);
}

public sealed class FrameBufferLayout
{
    public record struct AttachedFboIds(TextureId FboTexId, RenderBufferId RboDepthId, RenderBufferId RboTexId);

    private FrameBufferDesc _desc;

    internal FrameBufferLayout(FrameBufferId fboId, in AttachedFboIds attachedFboResources, in FrameBufferDesc desc)
    {
        FboId = fboId;
        AttachedFboResources = attachedFboResources;
        UpdateFromDescriptor(desc);
    }

    internal void UpdateFromDescriptor(in FrameBufferDesc desc)
    {
        _desc = desc;
    }

    internal FrameBufferDesc GetDescriptor() => _desc;

    public FrameBufferId FboId { get; }
    public AttachedFboIds AttachedFboResources { get; }
    public Vector2 SizeRatio => _desc.SizeRatio;
    public Vector2D<int> AbsoluteSize => _desc.AbsoluteSize;
    public bool DepthStencilBuffer => _desc.DepthStencilBuffer;
    public TexturePreset TexturePreset => _desc.TexturePreset;
    public bool Msaa => _desc.Msaa;
    public uint Samples => _desc.Samples;
    public bool AutoResizeable => _desc.AutoResizeable;
}

internal sealed class FrameBufferRepository : IFrameBufferRepository
{
    private readonly Dictionary<FrameBufferId, FrameBufferLayout> _registry = new(4);

    public FrameBufferLayout Get(FrameBufferId fboId)
    {
        return _registry[fboId];
    }

    public void AddRecord(FrameBufferId fboId, in FrameBufferLayout.AttachedFboIds attachedIds, in FrameBufferDesc desc)
    {
        fboId.IsValidOrThrow();
        _registry.Add(fboId, new FrameBufferLayout(fboId, in attachedIds, in desc));
    }

    internal FrameBufferDesc GetDescriptor(FrameBufferId fboId) => _registry[fboId].GetDescriptor();
}