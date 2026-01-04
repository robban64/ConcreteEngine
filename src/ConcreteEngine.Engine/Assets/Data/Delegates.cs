using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;

namespace ConcreteEngine.Engine.Assets.Data;

internal delegate TAsset LoadSimpleAssetDel<out TAsset, in TDesc>(AssetId id, TDesc manifest, AssetStore store)
    where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset LoadAssetDel<out TAsset, in TDesc>(TDesc manifest, ref LoadAssetContext ctx)
    where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset LoadEmbeddedAssetDel<out TAsset, in TEmbedded>(
    AssetId id,
    TEmbedded manifest,
    AssetStore store)
    where TAsset : AssetObject where TEmbedded : EmbeddedRecord;

internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFileSpec[] prevSpecs, out AssetFileSpec[] specs)
    where TAsset : AssetObject;