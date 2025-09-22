namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceContext
{
    IGfxResourceManager ResourceManager { get; }

    IGfxResourceRepository Repository { get; }

    IGfxResourceDisposer Disposer { get; }
}

internal sealed class GfxResourceContext : IGfxResourceContext
{
    private readonly GfxResourceManager _resourceManager;
    private readonly GfxResourceRepository _repository;
    private readonly GfxResourceDisposer _disposer;

    internal GfxResourceContext(GfxResourceManager resourceManager, GfxResourceRepository repository,
        GfxResourceDisposer disposer)
    {
        _resourceManager = resourceManager;
        _repository = repository;
        _disposer = disposer;
    }

    public IGfxResourceManager ResourceManager => _resourceManager;

    public IGfxResourceRepository Repository => _repository;

    public IGfxResourceDisposer Disposer => _disposer;
}