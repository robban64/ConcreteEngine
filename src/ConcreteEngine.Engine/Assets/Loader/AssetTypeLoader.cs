using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Asset;

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
    public AssetKind Kind => AssetEnums.ToAssetKind<TAsset>();
    public bool IsActive { get; protected set; }

    protected readonly AssetGfxUploader Uploader = uploader;

    public TAsset LoadAsset(TRecord record, ref LoaderContext ctx)
    {
        var asset = Load(record, ref ctx);

        if (ctx.Embedded?.Count > 0)
            ctx.Embedded?.Sort();

        return asset;
    }

    public abstract void Setup();
    public abstract void Teardown();

    protected abstract TAsset Load(TRecord record, ref LoaderContext ctx);
}