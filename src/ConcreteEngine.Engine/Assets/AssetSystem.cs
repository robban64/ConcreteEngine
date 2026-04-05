using System.Runtime;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.IO;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets;

public sealed class AssetSystem : GameEngineSystem
{
    internal AssetStore Store { get; }
    internal AssetFileRegistry FileRegistry { get; }
    internal AssetProviderImpl AssetProvider { get;}
    
    public MaterialStore MaterialStore { get; }
    public AssetProvider Provider => AssetProvider;

    private readonly AssetPendingQueue _pendingQueue;
    private AssetLoader? _loader;
    private AssetGfxUploader? _gfxUploader;
    
    public Status CurrentStatus { get; private set; } = Status.None;

    internal AssetSystem()
    {
        FileRegistry = new AssetFileRegistry();
        Store = new AssetStore(FileRegistry);
        MaterialStore = new MaterialStore(Store);
        AssetProvider = new AssetProviderImpl(Store, FileRegistry);

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

        var obj = Store.Get(command.Asset);
        if (obj is not Shader s)
            throw new NotImplementedException("Only shader reload is supported");

        _pendingQueue.Enqueue(new AssetRecreateRequest(s.GfxId, s.Id, AssetKind.Shader));
    }

    internal void ProcessPendingQueue(long frameId)
    {
        _pendingQueue.OnFrameStart(frameId);
        _pendingQueue.TryDrain(_loader!);
    }


    internal bool ProcessLoader() => _loader!.ProcessLoader();

    
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void StartLoader(GraphicsRuntime graphics)
    {
        InvalidOpThrower.ThrowIfNot(CurrentStatus == Status.ManifestLoaded, nameof(CurrentStatus));
        ArgumentNullException.ThrowIfNull(graphics);

        CurrentStatus = Status.Booting;

        _gfxUploader = new AssetGfxUploader(graphics.Gfx);
        _loader = new AssetLoader(Store, _gfxUploader);
        
        LoaderMetrics.Start();
        
        CreateFallbackAssets();
        AssetScanner.ScanAll(Store, FileRegistry, _loader.GetQueues());
        Store.EnsureStoreCapacity(_loader.GetQueues());

        var models = _loader.GetQueues()[AssetKind.Model.ToIndex()];
        graphics.Gfx.Meshes.EnsureMeshCount(models.Count);
        
        _loader.ActivateFullLoader();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void FinishLoading()
    {
        foreach (var it in Store.Collections) it.Sort();

        MaterialStore.InitializeStore();

        _loader?.DeactivateLoader();

        CurrentStatus = Status.Ready;
        LoaderMetrics.End();
        
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CreateFallbackAssets()
    {
        // Texture
        {
            var gid = Guid.Parse("196d3a4f-99e9-4d5a-971b-b42aa0012970");
            var textureId = Store.RegisterPlainAsset(gid, AssetKind.Texture, "White", AssetStorageKind.InMemory);
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
            var materialId = Store.RegisterPlainAsset(gid, AssetKind.Material, "Fallback", AssetStorageKind.InMemory);
            var material = MaterialLoader.CreateFallback(materialId, gid);
            MaterialStore.AddFallbackMaterial(material);
            Store.AddAsset(material);
        }
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

}