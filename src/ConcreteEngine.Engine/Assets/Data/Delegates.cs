using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;

namespace ConcreteEngine.Engine.Assets.Data;



internal delegate TAsset LoadSimpleAssetDel<out TAsset, in TDesc>(AssetId id, TDesc manifest, AssetStore store)
    where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset LoadAssetDel<out TAsset, in TDesc>(TDesc manifest, ref LoadAssetContext ctx)
    where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset LoadAdvancedAssetDel<out TAsset, in TDesc>(
    AssetId id,
    TDesc manifest,
    bool isCoreAsset,
    Action<ReadOnlySpan<IAssetEmbeddedDescriptor>> uploadEmbedded,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset LoadEmbeddedAssetDel<out TAsset, in TEmbedded>(
    AssetId id,
    TEmbedded manifest,
    AssetStore store)
    where TAsset : AssetObject where TEmbedded : class, IAssetEmbeddedDescriptor;

internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFileSpec[] prevFileSpecs,
    out AssetFileSpec[] fileSpecs)
    where TAsset : AssetObject;