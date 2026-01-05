using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Loader;


internal abstract class AssetTypeLoader<TAsset, TRecord>(AssetGfxUploader uploader)
    where TAsset : AssetObject where TRecord : AssetRecord
{
    public AssetKind Kind = AssetEnums.ToAssetKind<TAsset>();

    protected readonly AssetGfxUploader Uploader = uploader;

    public TAsset LoadAsset(TRecord record, LoaderContext ctx)
    {
        var asset = Load(record,ctx);

        if (ctx.Embedded?.Count > 0)
            ctx.Embedded?.Sort();

        return asset;    
    }
    
    protected abstract TAsset Load(TRecord record, LoaderContext ctx);
    protected abstract TAsset LoadEmbedded(EmbeddedRecord embedded, LoaderContext context);
    
    public abstract void Teardown();
    


}