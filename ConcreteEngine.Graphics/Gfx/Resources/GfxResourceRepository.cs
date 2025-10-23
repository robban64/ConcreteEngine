namespace ConcreteEngine.Graphics.Gfx.Resources;

public interface IGfxResourceRepository
{
    public IMeshRepository MeshRepository { get; }
}

//TODO Delete
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