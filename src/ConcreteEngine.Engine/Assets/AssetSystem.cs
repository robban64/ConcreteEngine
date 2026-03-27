using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.IO;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;
using static ConcreteEngine.Engine.Assets.Utils.AssetKindUtils;

namespace ConcreteEngine.Engine.Assets;

public sealed class AssetSystem : GameEngineSystem
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

    private AssetLoader? _loader;
    private AssetGfxUploader? _gfxUploader;

    private readonly AssetStore _store;
    private readonly MaterialStore _materialStore;

    private readonly AssetPendingQueue _pendingQueue;

    public Status CurrentStatus { get; private set; } = Status.None;

    public int PendingAssetCount => _pendingQueue.Count;

    public AssetStore Store => _store;
    public MaterialStore MaterialStore => _materialStore;


    internal AssetSystem()
    {
        _store = new AssetStore();
        _materialStore = new MaterialStore(_store);
        _pendingQueue = new AssetPendingQueue();
    }


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

        var obj = _store.Get(command.Asset);
        if (obj is not Shader s)
            throw new NotImplementedException("Only shader reload is supported");

        _pendingQueue.Enqueue(new AssetRecreateRequest(s.GfxId, s.Id, AssetKind.Shader));
    }

    internal void ProcessPendingQueue(long frameId)
    {
        _pendingQueue.OnFrameStart(frameId);

        while (_pendingQueue.TryDrain(out var rq))
        {
            try
            {
                ProcessRequest(in rq);
                Logger.LogString(LogScope.Engine, $"Recreating: {rq}");
            }
            catch (Exception ex)
            {
                var msg = $"{ex.GetType().Name}: Error while processing request {rq}";
                var level = ErrorUtils.IsUserOrDataError(ex) ? LogLevel.Warn : LogLevel.Critical;
                Logger.LogString(LogScope.Assets, msg, level);
                Logger.LogString(LogScope.Assets, ex.Message, level);

                if (ErrorUtils.IsUserOrDataError(ex) || ex is InvalidOperationException { InnerException: null } ||
                    ex is GraphicsException)
                {
                    continue;
                }

                throw;
            }
        }

        return;

        void ProcessRequest(in AssetRecreateRequest req)
        {
            switch (req.Kind)
            {
                case AssetKind.Shader: RecreateShader(req); break;
                case AssetKind.Model:
                case AssetKind.Texture:
                case AssetKind.Material:
                case AssetKind.Unknown:
                default:
                    throw new ArgumentException($"{req.Kind} is invalid for recreate", nameof(req.Kind));
            }
        }
    }

    private void RecreateShader(in AssetRecreateRequest req)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(req.AssetId.Value, 0, nameof(req.AssetId));
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)req.Kind, (int)AssetKind.Shader, nameof(req.Kind));

        if (_gfxUploader == null) throw new InvalidOperationException(nameof(_gfxUploader));
        if (_loader == null) throw new InvalidOperationException(nameof(_gfxUploader));

        var shader = _store.Get<Shader>(req.AssetId);
        if (!_loader.IsActive)
            _loader.ActivateLazyLoader();

        _loader.ReloadShader(shader);
    }

    internal bool ProcessLoader() => _loader!.ProcessLoader();

    private Stopwatch _loadTimer = new();
    private long _allocStart;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void StartLoader(GraphicsRuntime graphics)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(graphics);

        CurrentStatus = Status.Booting;
        _allocStart = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"Alloc Before loader: {_allocStart / 1000.0 / 1000.0}mb");
        _loadTimer.Start();

        _gfxUploader = new AssetGfxUploader(graphics.Gfx);
        _loader = new AssetLoader(_store, _gfxUploader);

        var scannedCount = AssetScanner.ScanAssetCount();
        _store.EnsureStoreCapacity(in scannedCount);
        CreateFallbackAssets();
        AssetScanner.ScanAll(in scannedCount, _store, _loader.GetQueues());

        var models = _loader.GetQueues()[ToIndex(AssetKind.Model)];
        graphics.Gfx.Meshes.EnsureMeshCount(models.Count);
        graphics.InitializeMeshScratchpad();

        _loader.ActivateFullLoader();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void FinishLoading()
    {
        foreach (var it in _store.Collections) it.Sort();

        _materialStore.InitializeStore();

        _loader?.DeactivateLoader();
        _loader = null;

        CurrentStatus = Status.Ready;
        _loadTimer.Stop();
        var alloc = GC.GetAllocatedBytesForCurrentThread() - _allocStart;

        var str = $"Asset load time: {_loadTimer.ElapsedTicks / 1000.0 / 1000.0}, Alloc: {alloc / 1000.0 / 1000.0}mb";
        Console.WriteLine(str);
        File.AppendAllText("diagnostic/load-time.txt", str + "\n");
        _loadTimer.Reset();
        _loadTimer = null!;
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    }

    private void CreateFallbackAssets()
    {
        // Texture
        {
            var gid = Guid.Parse("196d3a4f-99e9-4d5a-971b-b42aa0012970");
            var textureId = Store.RegisterScannedAsset(gid, 0);
            Store.AddAsset(new Texture("White")
            {
                Id = textureId,
                GId = gid,
                GfxId = GfxTextures.Fallback.AlbedoId,
                Size = new Size2D(1),
                TextureKind = TextureKind.Texture2D,
                Anisotropy = AnisotropyLevel.Off,
                Preset = TexturePreset.NearestClamp,
                PixelFormat = TexturePixelFormat.Rgba
            });
        }

        // Material
        {
            var gid = Guid.Parse("f28fbc18-9e84-41bf-b490-4b900b1d8598");
            var materialId = Store.RegisterScannedAsset(gid, 0);
            var material = MaterialLoader.CreateFallback(materialId, gid);
            _materialStore.AddFallbackMaterial(material);
            Store.AddAsset(material);
        }
    }
}