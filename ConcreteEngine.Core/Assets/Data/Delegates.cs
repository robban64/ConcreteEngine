#region

using ConcreteEngine.Core.Assets.Descriptors;

#endregion

namespace ConcreteEngine.Core.Assets.Data;

internal delegate TAsset AssetAssembleDel<out TAsset, in TDesc>(AssetId id, TDesc manifest, IAssetStore store)
    where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

internal delegate TAsset AssetFileAssembleDel<out TAsset, in TDesc>(
    AssetId id,
    TDesc manifest,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject where TDesc : class, IAssetDescriptor;

