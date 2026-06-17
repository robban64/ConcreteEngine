using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Assets;

public sealed class AssetSystem
{
    public Status CurrentStatus { get; private set; } = Status.None;

    private readonly AssetStore _assets;
    private readonly AssetManager _assetManager;
    private readonly AssetPendingQueue _pendingQueue;
    private readonly AssetLoader _loader;

    internal AssetSystem(GfxContext gfx)
    {
        _assetManager = AssetManager.Instance;
        _assets = _assetManager.Store;
        _pendingQueue = new AssetPendingQueue();
        _loader = new AssetLoader(_assets, gfx);
    }

    public int PendingAssetCount => _pendingQueue.Count;

    internal void Initialize()
    {
        if (CurrentStatus != Status.None)
            throw new InvalidOperationException("AssetSystem already initialized");

        CurrentStatus = Status.ManifestLoaded;
    }

    internal void EnqueueReloadAsset(AssetCommandRecord command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.Asset.IsValid()) throw new ArgumentException(nameof(command.Asset));
        _pendingQueue.Enqueue(new AssetRecreateRequest(command.Asset, command.Kind));
    }

    internal void ProcessPendingQueue()
    {
        _pendingQueue.TryDrain(_loader!, _assets);
    }

    internal bool ProcessLoader()
    {
        var finished =  _loader.ProcessLoader(out var finishedKind);
        if (finishedKind == AssetKind.Shader)
        {
            _assetManager.AttachShaders();
            AssetSystemSetup.CreateFallbackAssets(_assets);
        }
        
        return finished;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void StartLoader(GraphicsRuntime graphics)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(graphics);

        CurrentStatus = Status.Booting;

        AssetSystemSetup.Start();

        AssetScanner.ScanAll(_assets, _loader.GetQueues());
        _assets.EnsureStoreCapacity(_loader.GetQueues());

        var models = _loader.GetQueues()[AssetKind.Model.ToIndex()];
        graphics.Gfx.Meshes.EnsureMeshCount(models.Count);

        _loader.ActivateFullLoader();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void FinishLoading()
    {
        foreach (var it in _assets.GetTypeStoreSpan()) it.Sort();

        Shader.FallbackShader = _assets.GetByName<Shader>("Model");
        Material.FallbackMaterial.SetProfile(MaterialProfile.StaticModel);
        _loader?.DeactivateLoader();

        CurrentStatus = Status.Ready;
        AssetSystemSetup.End();
    }

    public enum Status
    {
        None = 0,
        ManifestLoaded = 1,
        Booting = 2,
        Ready = 3,
        Loading = 4,
        Unloaded = 5
    }

    public void Shutdown() { }
}