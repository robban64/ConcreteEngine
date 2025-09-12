namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBufferRegistry
{
    FrameBufferLayout Get(FrameBufferId fboId);
}

public sealed class FrameBufferLayout
{
    public required FrameBufferId FboId { get; init; }
    public TextureId FboTexId { get; init; }
    public RenderBufferId RboDepthId { get; init; }
    public RenderBufferId RboTexId { get; init; }
}

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