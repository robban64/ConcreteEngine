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

    private readonly AssetManager _assetManager;
    private readonly AssetPendingQueue _pendingQueue;
    private readonly AssetLoader _loader;
    private readonly AssetScanner _scanner;

    internal AssetSystem(GfxContext gfx)
    {
        _assetManager = AssetManager.Instance;
        _pendingQueue = new AssetPendingQueue();
        _loader = new AssetLoader(_assetManager, gfx);
        _scanner = new AssetScanner(_assetManager);
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
        if (_pendingQueue.Count == 0) return;
        _pendingQueue.TryDrain(_loader, _assetManager.Store);
    }

    internal bool ProcessLoader()
    {
        var finished =  _loader.ProcessLoader(out var finishedKind);
        if (finishedKind == AssetKind.Shader)
            _assetManager.AttachShaders();
        
        return finished;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void StartLoader(GraphicsRuntime graphics)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(graphics);

        CurrentStatus = Status.Booting;

        AssetSystemSetup.Start();

        _scanner.ScanAll(_loader.GetQueues());
        _assetManager.Store.EnsureStoreCapacity(_loader.GetQueues());

        var models = _loader.GetQueues()[AssetKind.Model.ToIndex()];
        graphics.Gfx.Meshes.EnsureMeshCount(models.Count);

        _loader.ActivateFullLoader();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void FinishLoading()
    {
        foreach (var it in _assetManager.Store.GetTypeStoreSpan()) it.Sort();

        _loader.DeactivateLoader();

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