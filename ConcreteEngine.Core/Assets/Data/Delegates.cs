namespace ConcreteEngine.Core.Assets.Data;

internal delegate TAsset AssetAssembleDel<out TAsset, in TManifest>(AssetId id, TManifest manifest, IAssetStore store)
    where TAsset : AssetObject where TManifest : class, IAssetManifestRecord;

internal delegate TAsset AssetFileAssembleDel<out TAsset, in TManifest>(
    AssetId id,
    TManifest manifest,
    out AssetFileSpec[] fileSpecs
) where TAsset : AssetObject where TManifest : class, IAssetManifestRecord;