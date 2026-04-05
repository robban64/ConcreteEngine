using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets.Loader;

internal interface IAssetTypeLoader
{
    AssetKind Kind { get; }
    bool IsActive { get; }
    void Setup(bool isSetup);
    void Teardown();
}

internal abstract class AssetTypeLoader<TAsset, TRecord>(AssetGfxUploader uploader) : IAssetTypeLoader
    where TAsset : AssetObject where TRecord : AssetRecord
{
    public AssetKind Kind => AssetKindUtils.ToAssetKind(typeof(TAsset));
    public abstract int SetupAllocSize { get; }
    public abstract int DefaultAllocSize { get; }

    public bool IsActive { get; private set; }
    public bool IsSetup { get; private set; }
    
    public readonly List<IEmbeddedAsset> EmbeddedAssets = [];

    protected readonly AssetGfxUploader Uploader = uploader;
    
    private ArenaAllocator? _allocator;
    
    public ArenaAllocator Allocator => _allocator ?? throw new  InvalidOperationException("Allocator is null");

    public TAsset LoadAsset(TRecord record, LoaderContext ctx)
    {
        if (!IsActive) throw new InvalidOperationException(nameof(IsActive));
        return record.LoadMode switch
        {
            AssetLoadingMode.Processed => Load(record, ctx),
            AssetLoadingMode.MemoryOnly => LoadInMemory(record, ctx),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void Setup(bool isSetup)
    {
        if(IsActive) throw new InvalidOperationException(nameof(IsActive));
        
        IsSetup = isSetup;
        IsActive = true;
        
        if (SetupAllocSize > 0 && DefaultAllocSize > 0)
        {
            var capacity = isSetup ? SetupAllocSize : DefaultAllocSize;
            _allocator = new ArenaAllocator(capacity, false);
        }

        OnSetup();
    }

    public void Teardown()
    {
        if(!IsActive) throw new InvalidOperationException(nameof(IsActive));

        _allocator?.Dispose();
        _allocator = null;
        IsActive = false;
        IsSetup = false;
        
        OnTeardown();
    }

    protected abstract void OnSetup();
    protected abstract void OnTeardown();

    protected abstract TAsset Load(TRecord record, LoaderContext ctx);
    protected abstract TAsset LoadInMemory(TRecord record, LoaderContext ctx);
}