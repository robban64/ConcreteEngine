namespace ConcreteEngine.Core.Assets.Data;

internal delegate TData AssetLoaderDel<in TManifest, out TData>(TManifest manifest) 
    where TManifest : class, IAssetManifestRecord;

internal delegate void AssetUploaderDel<TData, TResult>(in TData data, out TResult info) where TResult : struct;

internal delegate AssetFileId[] AssetRegisterFilesDel(AssetId assetId, ReadOnlySpan<AssetFileSpec> specs);

internal delegate TAsset AssetFileAssembleDel<out TAsset>(AssetId id, AssetRegisterFilesDel registerFiles)
    where TAsset : AssetObject;
    
internal delegate TAsset AssetAssembleDel<out TAsset>(AssetId id) where TAsset : AssetObject;

    