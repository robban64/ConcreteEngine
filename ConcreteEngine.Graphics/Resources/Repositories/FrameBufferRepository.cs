using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBufferRegistry
{
    FrameBufferLayout Get(FrameBufferId fboId);
}

public sealed record FrameBufferLayout(
    FrameBufferId FboId,
    FrameBufferLayout.AttachedFboIds AttachedFboResources,
    in FrameBufferDesc CreateDescriptor
)
{
    public record struct AttachedFboIds(TextureId FboTexId, RenderBufferId RboDepthId, RenderBufferId RboTexId);

    public FrameBufferDesc GetResizeDescriptor(Vector2D<int> size)
    {
        if (!CreateDescriptor.AutoResizeable)
            throw new GraphicsException($"Fbo {FboId} is not resizeable");

        return CreateDescriptor with { AbsoluteSize = size };
    }
}

public sealed class FrameBufferRepository : IFrameBufferRegistry
{
    private readonly Dictionary<FrameBufferId, FrameBufferLayout> _registry = new(4);

    public FrameBufferLayout Get(FrameBufferId fboId)
    {
        return _registry[fboId];
    }

    public void Register(FrameBufferLayout record)
    {
        _registry.Add(record.FboId, record);
    }
}