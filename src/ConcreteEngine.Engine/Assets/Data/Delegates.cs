using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;

namespace ConcreteEngine.Engine.Assets.Data;

internal delegate TAsset LoadEmbeddedAssetDel<out TAsset, in TEmbedded>(
    AssetId id,
    TEmbedded manifest,
    AssetStore store)
    where TAsset : AssetObject where TEmbedded : EmbeddedRecord;

internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFileSpec[] prevSpecs, out AssetFileSpec[] specs)
    where TAsset : AssetObject;