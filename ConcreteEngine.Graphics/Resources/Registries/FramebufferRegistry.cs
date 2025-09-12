namespace ConcreteEngine.Graphics.Resources;

public interface IFrameBufferRegistry
{
    IMeshLayout Get(MeshId meshId);
}

public sealed class FrameBufferLayout
{
    public required FrameBufferId FboId { get; init; }
    public TextureId TexColorId { get; init; }
    public RenderBufferId RboTexId { get; init; }
    public RenderBufferId RboDepthId { get; init; }
}

public sealed class FrameBufferRegistry
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