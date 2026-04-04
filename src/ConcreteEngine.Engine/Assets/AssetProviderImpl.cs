using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetProviderImpl(AssetStore assets, AssetFileRegistry files) : AssetProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override AssetObject Get(AssetId id) => assets.Get(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Get<T>(AssetId id) => assets.Get<T>(id);
    public override T Get<T>(string name) => assets.GetByName<T>(name);

    public override bool TryGet<T>(AssetId id, out T asset) => assets.TryGet(id, out asset);
    public override bool TryGet<T>(string name, out T asset) => assets.TryGetByName(name, out asset);
    public override bool TryGet<T>(Guid gid, out T asset) => assets.TryGetByGuid(gid, out asset);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<AssetObject> GetAllAssets() => assets.GetAllAssets();
    public override ReadOnlySpan<AssetId> GetAssetIdsByKind(AssetKind kind) => assets.GetAssetList(kind).AsSpan();

    public override bool IsUnboundFile(AssetFileId fileId) => files.IsUnboundFile(fileId);
    public override bool IsRootFile(AssetFileId fileId) => files.IsRootFile(fileId);

    public override AssetFileSpec GetFile(AssetFileId id) => files.Get(id);
    public override AssetFileSpec GetAssetRootFile(AssetId id) => files.Get(files.GetFileBindings(id)[0]);

    public override bool TryGetByRootFile(AssetFileId id, out AssetObject asset)
    {
        asset = null!;
        if (!files.TryGetByRootFileId(id, out var assetId)) return false;
        return assets.TryGet(assetId, out asset);
    }

    public override ReadOnlySpan<AssetFileId> GetAssetFileBindings(AssetId id) => files.GetFileBindings(id);
    public override ReadOnlySpan<AssetFileId> GetUnboundFileIds() => files.GetUnboundFileIds();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override AssetEnumerator AssetEnumerator(AssetKind kind) => assets.GetAssetEnumerator(kind);
}