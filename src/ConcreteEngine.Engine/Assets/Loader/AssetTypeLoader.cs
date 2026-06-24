using System.Diagnostics;
using ConcreteEngine.Core.Common;
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

    public TAsset LoadAsset(TRecord record, ImportContext ctx)
    {
        if (!IsActive) Throwers.InvalidOperation(nameof(IsActive));
        return record.LoadMode switch
        {
            AssetLoadingMode.Processed => Load(record, ctx),
            AssetLoadingMode.MemoryOnly => LoadInMemory(record, ctx),
            _ => throw new UnreachableException()
        };
    }

    public void Activate(bool isSetup)
    {
        if (IsActive) Throwers.InvalidOperation(nameof(IsActive));

        IsSetup = isSetup;
        IsActive = true;

        OnActivate();
    }

    public void DeActivate()
    {
        if (!IsActive) Throwers.InvalidOperation(nameof(IsActive));

        IsActive = false;
        IsSetup = false;

        OnDeActivate();
    }


    protected abstract void OnActivate();
    protected abstract void OnDeActivate();

    protected abstract TAsset Load(TRecord record, ImportContext ctx);
    protected abstract TAsset LoadInMemory(TRecord record, ImportContext ctx);

    public virtual void Reload(TAsset asset, AssetFile[] files) => throw new NotImplementedException();
}