namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceRegistry
{
    public IShaderRepository ShaderRepository { get; }
    public IMeshRepository MeshRepository { get; }
    public IFrameBufferRepository FboRepository { get; }
}

internal sealed class GfxResourceRegistry : IGfxResourceRegistry
{
    private readonly ShaderRepository _shaderRepository;
    private readonly MeshRepository _meshRepository;
    private readonly FrameBufferRepository _fboRepository;

    public ShaderRepository ShaderRepository => _shaderRepository;
    public MeshRepository MeshRepository => _meshRepository;
    public FrameBufferRepository FboRepository => _fboRepository;

    IShaderRepository IGfxResourceRegistry.ShaderRepository => _shaderRepository;
    IMeshRepository IGfxResourceRegistry.MeshRepository => _meshRepository;
    IFrameBufferRepository IGfxResourceRegistry.FboRepository => _fboRepository;

    public GfxResourceRegistry(IGfxResourceManager resources)
    {
        _shaderRepository = new ShaderRepository(resources);
        _meshRepository = new MeshRepository();
        _fboRepository = new FrameBufferRepository();
    }

}