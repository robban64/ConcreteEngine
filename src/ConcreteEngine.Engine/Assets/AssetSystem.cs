using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;

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

    private readonly AssetScanner _scanner;
    private readonly AssetPendingQueue _pendingQueue;

    public Status CurrentStatus { get; private set; } = Status.None;

    public int PendingAssetCount => _pendingQueue.Count;

    public AssetStore Store => _store;
    public MaterialStore MaterialStore => _materialStore;


    internal AssetSystem()
    {
        _store = new AssetStore();
        _materialStore = new MaterialStore(_store);
        _scanner = new AssetScanner();
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

        var shader = _store.Get<Shader>(req.AssetId);
        _loader ??= new AssetLoader();
        if (!_loader.IsActive)
            _loader.ActivateLazyLoader(_store, _gfxUploader);

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

        //_scanner.ScanDirectory(EnginePath.AssetRoot);

        _loader = new AssetLoader();
        _gfxUploader = new AssetGfxUploader(graphics.Gfx);

        var recordQueue = _scanner.ScanEnqueueDirectory(_store, EnginePath.AssetRoot);

        var models = recordQueue[(int)AssetKind.Model - 1];
        graphics.Gfx.Meshes.EnsureMeshCount(models.Count);
        graphics.InitializeMeshScratchpad();

        _loader.ActivateFullLoader(_store, _gfxUploader, recordQueue);
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
        //File.AppendAllText("diagnostic/load-time.txt", str + "\n");
        _loadTimer.Reset();
        _loadTimer = null!;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    }
}