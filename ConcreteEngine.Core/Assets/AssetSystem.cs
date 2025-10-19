#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Graphics.Gfx;

#endregion

namespace ConcreteEngine.Core.Assets;

public interface IAssetSystem : IGameEngineSystem
{
    IAssetStore Store { get; }
    IMaterialStore MaterialStore { get; }
}

public sealed class AssetSystem : IAssetSystem
{
    public enum Status
    {
        None = 0,
        ManifestLoaded = 1,
        Booting = 2,
        Ready = 3,
        Loading = 4,
        Unloaded = 5
    }

    private AssetConfigLoader? _configLoader;
    private AssetStartupWorker? _processor;
    private AssetGfxUploader? _uploader;
    private AssetLoader? _loader;

    private AssetManifest _manifest = null!;

    private readonly MaterialStore _materialStore;

    private readonly AssetStore _assetStore;

    public Status CurrentStatus { get; private set; } = Status.None;

    internal AssetSystem()
    {
        _assetStore = new AssetStore();
        _materialStore = new MaterialStore(_assetStore);
    }

    internal AssetStore InternalStore => _assetStore;
    public IAssetStore Store => _assetStore;
    public IMaterialStore MaterialStore => _materialStore;

    internal MaterialStore Materials => _materialStore;

    internal void Initialize()
    {
        if (CurrentStatus != Status.None)
            throw new InvalidOperationException("AssetSystem already initialized");

        _configLoader = new AssetConfigLoader();
        _manifest = _configLoader.LoadAssetManifest();

        CurrentStatus = Status.ManifestLoaded;
    }


    internal void StartLoader(GfxContext gfx)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(gfx, nameof(gfx));
        ArgumentNullException.ThrowIfNull(_configLoader, nameof(_configLoader));

        CurrentStatus = Status.Booting;

        _uploader = new AssetGfxUploader(gfx);
        _loader = new AssetLoader();
        _processor = new AssetStartupWorker(_loader, _configLoader, _manifest);
        _processor.Start(_assetStore, _uploader);
    }

    internal bool ProcessLoader(int n)
    {
        if (_loader is null || _processor is null)
            throw new InvalidOperationException("Asset loaders are not fully initialized");

        return _processor.ProcessAssets(n);
    }

    internal void FinishLoading()
    {
        _materialStore.InitializeStore();

        _processor?.Finish();
        _processor = null;

        _loader?.DeactivateLoader();
        _loader = null;

        _uploader = null;
        _configLoader = null;

        CurrentStatus = Status.Ready;
    }

    public void Shutdown()
    {
        CurrentStatus = Status.Unloaded;
    }
}