#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Diagnostic;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Core.Worlds.Render;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;

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

    private readonly ResourcePendingQueue _pendingQueue;

    private AssetLoader? _loader;
    private AssetConfigLoader? _configLoader;
    private AssetStartupWorker? _processor;

    private AssetGfxUploader _gfxUploader = null!;

    private AssetManifest _manifest = null!;

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;

    public Status CurrentStatus { get; private set; } = Status.None;
    
    public int PendingAssetCount => _pendingQueue.Count;

    internal AssetSystem()
    {
        _assetStore = new AssetStore();
        _materialStore = new MaterialStore(_assetStore);
        _pendingQueue = new ResourcePendingQueue();
    }

    internal AssetStore StoreImpl => _assetStore;
    public IAssetStore Store => _assetStore;


    internal MaterialStore MaterialStoreImpl => _materialStore;
    public IMaterialStore MaterialStore => _materialStore;


    internal void Initialize()
    {
        if (CurrentStatus != Status.None)
            throw new InvalidOperationException("AssetSystem already initialized");

        _configLoader = new AssetConfigLoader();
        _manifest = _configLoader.LoadAssetManifest();

        CurrentStatus = Status.ManifestLoaded;
    }

    internal void EnqueueRecreateShader(string name)
    {
        if (!_assetStore.TryGetByName(name, typeof(Shader), out var obj) || obj is not Shader s) return;
        _pendingQueue.Enqueue(new RecreateRequest(s.ResourceId, s.RawId, AssetKind.Shader));
    }


    internal void UpdatePendingQueue(long frameIndex)
    {
        _pendingQueue.OnFrameStart(frameIndex);
    }

    internal void ProcessPendingQueue()
    {
        while (_pendingQueue.TryDrain(out var req))
        {
            if (req.Kind == AssetKind.Shader)
                RecreateShader(req);
            else throw new InvalidOperationException($"Unsupported asset kind {req.Kind}");
        }
    }

    private void RecreateShader(in RecreateRequest req)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(req.AssetId.Value, 0, nameof(req.AssetId));
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)req.Kind, (int)AssetKind.Shader, nameof(req.Kind));
        InvalidOpThrower.ThrowIfNull(_gfxUploader, nameof(_gfxUploader));

        var shader = _assetStore.GetByRef(AssetRef<Shader>.Make(req.AssetId));
        _loader ??= new AssetLoader();
        if (!_loader.IsActive)
            _loader.ActivateLazyLoader(_assetStore, _gfxUploader);

        try
        {
            _loader.ReloadShader(shader);
        }
        catch (Exception e) when(ErrorUtils.IsUserOrDataError(e) || ErrorUtils.IsGfxError(e))
        {
            Console.WriteLine(ErrorUtils.ErrorMessageFor(e));
        }
    }


    internal void StartLoader(GfxContext gfx)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(gfx, nameof(gfx));
        ArgumentNullException.ThrowIfNull(_configLoader, nameof(_configLoader));

        CurrentStatus = Status.Booting;

        _gfxUploader = new AssetGfxUploader(gfx);
        _loader = new AssetLoader();
        _processor = new AssetStartupWorker(_loader, _configLoader, _manifest);
        _processor.Start(_assetStore, _gfxUploader);
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

        _configLoader = null;

        CurrentStatus = Status.Ready;
    }

    public void Shutdown()
    {
        CurrentStatus = Status.Unloaded;
    }
}