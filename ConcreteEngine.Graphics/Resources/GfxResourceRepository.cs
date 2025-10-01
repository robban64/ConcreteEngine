namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceRepository
{
    public IMeshRepository MeshRepository { get; }
}

internal sealed class GfxResourceRepository : IGfxResourceRepository
{
    private readonly MeshRepository _meshRepository;

    public MeshRepository MeshRepository => _meshRepository;

    IMeshRepository IGfxResourceRepository.MeshRepository => _meshRepository;

    public GfxResourceRepository(GfxResourceManager resources)
    {
        _meshRepository = new MeshRepository();
    }
}