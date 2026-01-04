using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Loader;


internal abstract class AssetTypeLoader<TAsset> : IDisposable where TAsset : AssetObject
{
    public AssetKind Kind = AssetEnums.ToAssetKind<TAsset>();
    
    public TAsset LoadAsset(LoaderContext ctx)
    {
        if (ctx.Record.Kind != Kind)
            throw new InvalidOperationException($"Loader {Kind} cannot load record of kind {ctx.Record.Kind}");
        
        var asset = Load(ctx);

        if (ctx.Embedded?.Count > 0)
            ctx.Embedded?.Sort();

        return asset;    
    }

    protected abstract TAsset Load(LoaderContext context);

    public virtual void Dispose() { }
}