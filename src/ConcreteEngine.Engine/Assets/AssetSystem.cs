using System.Diagnostics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Command;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;

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

    private readonly AssetPendingQueue _pendingQueue;

    private AssetLoader? _loader;
    private AssetManifestProvider? _configLoader;
    private AssetStartupWorker? _processor;

    private AssetGfxUploader _gfxUploader = null!;


    private readonly AssetStore _store;
    private readonly MaterialStore _materialStore;

    private readonly AssetScanner _scanner;

    public Status CurrentStatus { get; private set; } = Status.None;

    public int PendingAssetCount => _pendingQueue.Count;

    internal AssetSystem()
    {
        _store = new AssetStore();
        _materialStore = new MaterialStore(_store);
        _scanner = new AssetScanner(_store);
        _pendingQueue = new AssetPendingQueue();
    }

    public AssetStore Store => _store;
    public MaterialStore MaterialStore => _materialStore;


    internal void Initialize()
    {
        if (CurrentStatus != Status.None)
            throw new InvalidOperationException("AssetSystem already initialized");

        _configLoader = new AssetManifestProvider();
        _configLoader.LoadManifest();
        CurrentStatus = Status.ManifestLoaded;
    }

    internal void EnqueueReloadAsset(AssetCommandRecord command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name, nameof(command.Name));

        if (!_store.TryGetByName(command.Name, typeof(Shader), out var obj) || obj is not Shader s)
            throw new KeyNotFoundException($"No shader found with name {command.Name}");

        _pendingQueue.Enqueue(new AssetRecreateRequest(s.ResourceId, s.Id, AssetKind.Shader));
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
        InvalidOpThrower.ThrowIfNull(_gfxUploader, nameof(_gfxUploader));

        var shader = _store.GetByRef(AssetRef<Shader>.Make(req.AssetId));
        _loader ??= new AssetLoader();
        if (!_loader.IsActive)
            _loader.ActivateLazyLoader(_store, _gfxUploader);

        _loader.ReloadShader(shader);
    }

    private Stopwatch _loadTimer = new();
    private long _allocStart = 0;

    internal void StartLoader(GfxContext gfx)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(gfx);
        ArgumentNullException.ThrowIfNull(_configLoader);

        CurrentStatus = Status.Booting;
        _allocStart = GC.GetAllocatedBytesForCurrentThread();
        _loadTimer.Start();
        
        _scanner.ScanDirectory(EnginePath.AssetRoot);

        _loader = new AssetLoader();
        _processor = new AssetStartupWorker(_loader, _configLoader);
        _gfxUploader = new AssetGfxUploader(gfx);
        _processor.Start(_store, _gfxUploader);
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
    }
}