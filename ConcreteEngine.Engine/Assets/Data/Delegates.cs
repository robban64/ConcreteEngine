#region

using ConcreteEngine.Engine.Assets.Descriptors;

#endregion

namespace ConcreteEngine.Engine.Assets.Data;

internal delegate TAsset AssetAssembleDel<out TAsset, in TDesc>(AssetId id, TDesc manifest, IAssetStore store)
    where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset AssetFileAssembleDel<out TAsset, in TDesc>(
    AssetId id,
    TDesc manifest,
    bool isCoreAsset,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate void AssetFileReloadDel<in TAsset>(
    TAsset asset,
    AssetFileEntry[] files,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject;