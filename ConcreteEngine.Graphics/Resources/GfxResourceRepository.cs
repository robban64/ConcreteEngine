namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceRepository
{
    public IShaderRepository ShaderRepository { get; }
    public IMeshRepository MeshRepository { get; }
    public IFrameBufferRepository FboRepository { get; }
}

internal sealed class GfxResourceRepository : IGfxResourceRepository
{
    private readonly ShaderRepository _shaderRepository;
    private readonly MeshRepository _meshRepository;
    private readonly FrameBufferRepository _fboRepository;

    public ShaderRepository ShaderRepository => _shaderRepository;
    public MeshRepository MeshRepository => _meshRepository;
    public FrameBufferRepository FboRepository => _fboRepository;

    IShaderRepository IGfxResourceRepository.ShaderRepository => _shaderRepository;
    IMeshRepository IGfxResourceRepository.MeshRepository => _meshRepository;
    IFrameBufferRepository IGfxResourceRepository.FboRepository => _fboRepository;

    public GfxResourceRepository(GfxResourceManager resources)
    {
        _shaderRepository = new ShaderRepository(resources.GfxStoreHub);
        _meshRepository = new MeshRepository();
        _fboRepository = new FrameBufferRepository();
    }
}