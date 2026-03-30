using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetProviderImpl(AssetStore assets, AssetFileRegistry files) : AssetProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override AssetObject GetAsset(AssetId id) => assets.Get(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T GetAsset<T>(AssetId id) => assets.Get<T>(id);

    public override bool TryGetAsset<T>(AssetId id, out T asset) => assets.TryGet(id, out asset);
    public override bool TryGetAssetByName<T>(string name, out T asset) => assets.TryGetByName(name, out asset);
    public override bool TryGetByGuid<T>(Guid gid, out T asset) => assets.TryGetByGuid(gid, out asset);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<AssetObject> GetAllAssets() => assets.GetAllAssets();

    public override bool IsRootFile(AssetFileId fileId) => !files.IsUnboundFile(fileId);

    public override bool TryGetByRootFile(AssetFileId id, out AssetObject asset)
    {
        asset = null!;
        if (!files.TryGetByRootFileId(id, out var assetId)) return false;
        return assets.TryGet(assetId, out asset);
    }

    public override ReadOnlySpan<AssetFileId> GetAssetFileBindings(AssetId id) => files.GetAssetFileBindings(id);
    public override ReadOnlySpan<AssetFileId> GetUnboundFileIds() => files.GetUnboundFileIds();

    public override AssetFileSpec GetFileSpec(AssetFileId id) => files.Get(id);

    public override AssetFileSpec GetAssetRootFile(AssetId id)
    {
        files.TryGetFile(files.GetAssetFileBindings(id)[0], out var file);
        return file;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override AssetEnumerator AssetEnumerator(AssetKind kind) => assets.GetAssetEnumerator(kind);


}