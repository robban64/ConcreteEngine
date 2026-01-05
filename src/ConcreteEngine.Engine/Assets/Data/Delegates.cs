using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;

namespace ConcreteEngine.Engine.Assets.Data;


internal delegate TAsset LoadEmbeddedAssetDel<out TAsset, in TEmbedded>(
    AssetId id,
    TEmbedded manifest,
    AssetStore store)
    where TAsset : AssetObject where TEmbedded : EmbeddedRecord;

internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFileSpec[] prevSpecs, out AssetFileSpec[] specs)
    where TAsset : AssetObject;