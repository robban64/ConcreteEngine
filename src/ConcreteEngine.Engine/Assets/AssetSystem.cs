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

    public AssetStore Assets => AssetStore.Instance;

    private readonly AssetPendingQueue _pendingQueue;
    private readonly AssetLoader _loader;

    internal AssetSystem(GfxContext gfx)
    {
        _pendingQueue = new AssetPendingQueue();
        _loader = new AssetLoader(Assets, gfx);
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
        AssetSystemSetup.CreateFallbackAssets(Assets);

        AssetScanner.ScanAll(Assets, _loader.GetQueues());
        Assets.EnsureStoreCapacity(_loader.GetQueues());

        var models = _loader.GetQueues()[AssetKind.Model.ToIndex()];
        graphics.Gfx.Meshes.EnsureMeshCount(models.Count);

        _loader.ActivateFullLoader();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void FinishLoading()
    {
        foreach (var it in Assets.Collections) it.Sort();

        Material.FallbackMaterial.BoundShader = Assets.GetByName<Shader>("Model");
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