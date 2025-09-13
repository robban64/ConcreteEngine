namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBufferRegistry
{
    FrameBufferLayout Get(FrameBufferId fboId);
}

public sealed record FrameBufferLayout(
    FrameBufferId FboId,
    TextureId FboTexId,
    RenderBufferId RboDepthId,
    RenderBufferId RboTexId);
public sealed class FrameBufferRegistry : IFrameBufferRegistry
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