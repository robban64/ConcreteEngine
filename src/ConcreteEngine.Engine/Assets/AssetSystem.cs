using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Shaders;
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

    public AssetStore Store => _assetStore;
    public MaterialStore MaterialStore => _materialStore;


    internal void Initialize()
    {
        if (CurrentStatus != Status.None)
            throw new InvalidOperationException("AssetSystem already initialized");

        _configLoader = new AssetConfigLoader();
        _manifest = _configLoader.LoadAssetManifest();

        CurrentStatus = Status.ManifestLoaded;
    }

    internal void EnqueueReloadAsset(AssetCommandRecord command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name, nameof(command.Name));

        if (!_assetStore.TryGetByName(command.Name, typeof(Shader), out var obj) || obj is not Shader s)
            throw new KeyNotFoundException($"No shader found with name {command.Name}");

        _pendingQueue.Enqueue(new RecreateRequest(s.ResourceId, s.RawId, AssetKind.Shader));
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

        void ProcessRequest(in RecreateRequest req)
        {
            switch (req.Kind)
            {
                case AssetKind.Shader: RecreateShader(req); break;
                case AssetKind.Model:
                case AssetKind.Texture2D:
                case AssetKind.TextureCubeMap:
                case AssetKind.MaterialTemplate:
                case AssetKind.Unknown:
                default:
                    throw new ArgumentException($"{req.Kind} is invalid for recreate", nameof(req.Kind));
            }
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

        _loader.ReloadShader(shader);
    }


    internal void StartLoader(GfxContext gfx)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(gfx);
        ArgumentNullException.ThrowIfNull(_configLoader);

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
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

}