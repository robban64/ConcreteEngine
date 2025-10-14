#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Graphics.Gfx;

#endregion

namespace ConcreteEngine.Core.Assets;

public interface IAssetSystem : IGameEngineSystem
{
    IAssetStore Store { get; }
}

public sealed class AssetSystem : IAssetSystem
{
    public static bool Initialized { get; private set; } = false;
    public static bool IsLoading { get; private set; } = false;

    private readonly AssetStore _assetStore = new();

    private AssetConfigLoader? _configLoader;
    private AssetStartupWorker? _processor;
    private AssetGfxUploader? _uploader;
    private AssetLoader? _loader;

    private AssetManifest _manifest = null!;
    private MaterialStore _materialStore = null!;

    internal AssetStore AssetStore => _assetStore;

    public IAssetStore Store => _assetStore;

    public MaterialStore MaterialStore => _materialStore;


    internal AssetSystem()
    {
    }

    internal void Initialize()
    {
        if (Initialized)
            throw new InvalidOperationException("AssetSystem already initialized");

        _configLoader = new AssetConfigLoader();
        _manifest = _configLoader.LoadAssetManifest();

        Initialized = true;
    }


    internal void StartLoader(GfxContext gfx)
    {
        InvalidOpThrower.ThrowIfNot(Initialized, nameof(Initialized));
        ArgumentNullException.ThrowIfNull(gfx, nameof(gfx));
        ArgumentNullException.ThrowIfNull(_configLoader, nameof(_configLoader));

        InvalidOpThrower.ThrowIf(IsLoading, nameof(IsLoading));

        IsLoading = true;

        _uploader = new AssetGfxUploader(gfx);
        _loader = new AssetLoader();
        _processor = new AssetStartupWorker(_loader, _configLoader, _manifest);
        _processor.Start(_assetStore, _uploader);
    }

    internal bool ProcessLoader(int n)
    {
        if (_loader == null)
            throw new InvalidOperationException("Asset loader is not initialized");

        for (var i = 0; i < n; i++)
        {
            if (_processor!.ProcessGfxAssets()) return true;
        }

        return false;
    }

    internal MaterialStore FinishLoading()
    {
        _materialStore = _processor!.ProcessMaterials();
        _processor.Finish();
        _processor = null;
        
        IsLoading = false;
        return _materialStore;
    }

    public void Shutdown()
    {
        /*
        foreach (var asset in _store.Values)
            if (asset is IDisposable disposable)
                disposable.Dispose();
                */
    }
}