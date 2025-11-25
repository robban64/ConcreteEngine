#region

using ConcreteEngine.Engine.Assets.Descriptors;

#endregion

namespace ConcreteEngine.Engine.Assets.Data;

internal delegate TAsset AssetAssembleDel<out TAsset, in TDesc>(AssetId id, TDesc manifest, AssetStore store)
    where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset EmbeddedAssembleDel<out TAsset, in TEmbedded>(AssetId id, TEmbedded manifest, AssetStore store)
    where TAsset : AssetObject where TEmbedded : class, IAssetEmbeddedDescriptor;


internal delegate TAsset AssetFileAssembleDel<out TAsset, in TDesc>(
    AssetId id,
    TDesc manifest,
    bool isCoreAsset,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset AssetWithEmbeddedDel<out TAsset, in TDesc>(
    AssetId id,
    TDesc manifest,
    bool isCoreAsset,
    Action<ReadOnlySpan<IAssetEmbeddedDescriptor>> uploadEmbedded,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject where TDesc : class, IAssetDescriptor;




internal delegate void AssetFileReloadDel<in TAsset>(
    TAsset asset,
    AssetFileEntry[] files,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject;