using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Assets;

public sealed class AssetSystem : IGameEngineSystem
{
    public Status CurrentStatus { get; private set; } = Status.None;

    public AssetStore Assets { get; }
    public AssetFileRegistry Files { get; }
    public MaterialStore MaterialStore { get; }

    private readonly AssetPendingQueue _pendingQueue;
    private AssetLoader? _loader;

    internal AssetSystem()
    {
        Files = new AssetFileRegistry();
        Assets = new AssetStore(Files);
        MaterialStore = new MaterialStore(Assets);

        _pendingQueue = new AssetPendingQueue();
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
        _pendingQueue.TryDrain(_loader!, Assets);
    }

    internal bool ProcessLoader() => _loader!.ProcessLoader();


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void StartLoader(GraphicsRuntime graphics)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(graphics);

        CurrentStatus = Status.Booting;

        AssetSystemSetup.Start();
        AssetSystemSetup.CreateFallbackAssets(Assets, MaterialStore);

        _loader = new AssetLoader(Assets, graphics.Gfx);

        AssetScanner.ScanAll(Assets, Files, _loader.GetQueues());
        Assets.EnsureStoreCapacity(_loader.GetQueues());

        var models = _loader.GetQueues()[AssetKind.Model.ToIndex()];
        graphics.Gfx.Meshes.EnsureMeshCount(models.Count);

        _loader.ActivateFullLoader();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void FinishLoading()
    {
        foreach (var it in Assets.Collections) it.Sort();

        MaterialStore.InitializeStore();
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