namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceRegistry
{
    public IShaderRegistry ShaderRegistry { get; }
    public IMeshRegistry MeshRegistry { get; }
    public IFrameBufferRegistry FboRegistry { get; }
}

internal sealed class GfxResourceRegistry : IGfxResourceRegistry
{
    private readonly ShaderRegistry _shaderRegistry;
    private readonly MeshRegistry _meshRegistry;
    private readonly FrameBufferRegistry _fboRegistry;

    public ShaderRegistry ShaderRegistry => _shaderRegistry;
    public MeshRegistry MeshRegistry => _meshRegistry;
    public FrameBufferRegistry FboRegistry => _fboRegistry;

    IShaderRegistry IGfxResourceRegistry.ShaderRegistry => _shaderRegistry;
    IMeshRegistry IGfxResourceRegistry.MeshRegistry => _meshRegistry;
    IFrameBufferRegistry IGfxResourceRegistry.FboRegistry => _fboRegistry;

    public GfxResourceRegistry(IGfxResourceManager resources)
    {
        _shaderRegistry = new ShaderRegistry(resources);
        _meshRegistry = new MeshRegistry();
        _fboRegistry = new FrameBufferRegistry();
    }

}