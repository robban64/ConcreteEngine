using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets.Loader;

internal interface IAssetTypeLoader
{
    bool IsActive { get; }
    
    void Activate(bool isSetup);
    void DeActivate();
    
    static abstract AssetKind Kind { get; }
}

internal interface IAssetTypeLoader<in TAsset> : IAssetTypeLoader where TAsset : AssetObject
{
    void Reload(TAsset asset, AssetFile[] files);
}

internal abstract class AssetTypeLoader<TAsset, TRecord> : IAssetTypeLoader<TAsset>
    where TAsset : AssetObject where TRecord : AssetRecord
{
    public bool IsActive { get; private set; }
    public bool IsSetup { get; private set; }

    public static AssetKind Kind => AssetKindUtils.ToAssetKind(typeof(TAsset));

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

    public void Activate(bool isSetup)
    {
        if (IsActive) throw new InvalidOperationException(nameof(IsActive));

        IsSetup = isSetup;
        IsActive = true;

        OnActivate();
    }

    public void DeActivate()
    {
        if (!IsActive) throw new InvalidOperationException(nameof(IsActive));

        IsActive = false;
        IsSetup = false;

        OnDeActivate();
    }


    protected abstract void OnActivate();
    protected abstract void OnDeActivate();

    protected abstract TAsset Load(TRecord record, LoaderContext ctx);
    protected abstract TAsset LoadInMemory(TRecord record, LoaderContext ctx);

    public virtual void Reload(TAsset asset, AssetFile[] files) => throw new NotImplementedException();
}