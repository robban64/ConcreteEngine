namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceRepository
{
    public IShaderRepository ShaderRepository { get; }
    public IMeshRepository MeshRepository { get; }
}

internal sealed class GfxResourceRepository : IGfxResourceRepository
{
    private readonly ShaderRepository _shaderRepository;
    private readonly MeshRepository _meshRepository;

    public ShaderRepository ShaderRepository => _shaderRepository;
    public MeshRepository MeshRepository => _meshRepository;

    IShaderRepository IGfxResourceRepository.ShaderRepository => _shaderRepository;
    IMeshRepository IGfxResourceRepository.MeshRepository => _meshRepository;

    public GfxResourceRepository(GfxResourceManager resources)
    {
        _shaderRepository = new ShaderRepository(resources.GfxStoreHub);
        _meshRepository = new MeshRepository();
    }
}