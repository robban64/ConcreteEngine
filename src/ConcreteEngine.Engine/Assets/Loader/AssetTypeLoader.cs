using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Utils;

namespace ConcreteEngine.Engine.Assets.Loader;

internal interface IAssetTypeLoader
{
    AssetKind Kind { get; }
    bool IsActive { get; }
    void Setup();
    void Teardown();
}

internal abstract class AssetTypeLoader<TAsset, TRecord>(AssetGfxUploader uploader) : IAssetTypeLoader
    where TAsset : AssetObject where TRecord : AssetRecord
{
    public AssetKind Kind => AssetKindUtils.ToAssetKind<TAsset>();
    public bool IsActive { get; protected set; }

    protected readonly AssetGfxUploader Uploader = uploader;

    public List<IEmbeddedAsset> EmbeddedAssets = [];

    public TAsset LoadAsset(TRecord record, LoaderContext ctx)
    {
        if (!IsActive) throw new InvalidOperationException(nameof(IsActive));
        var asset = Load(record, ctx);
        return asset;
    }

    public abstract void Setup();
    public abstract void Teardown();

    protected abstract TAsset Load(TRecord record, LoaderContext ctx);
}