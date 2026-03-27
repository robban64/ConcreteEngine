using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class AssetApiController(ApiContext context) : AssetController
{
    private readonly AssetStore _store = context.AssetStore;

    public override AssetObject GetAsset(AssetId id) => _store.Get(id);
    public override T GetAsset<T>(AssetId id) => _store.Get<T>(id);

    public override bool TryGetAsset(AssetId id, out AssetObject asset) => _store.TryGet(id, out asset);
    public override bool TryGetAsset<T>(AssetId id, out T asset) => _store.TryGet<T>(id, out asset);
    public override bool TryGetByGuid<T>(Guid gid, out T asset) => _store.TryGetByGuid<T>(gid, out asset);

    public override ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind) =>
        _store.GetAssetList(kind).GetAssetObjectSpan();

    public override ReadOnlySpan<T> GetAssetSpan<T>() => _store.GetAssetList<T>().GetAssetSpan();

    public override AssetFileSpec[] GetAssetFileSpecs(AssetId assetId)
    {
        _store.TryGetFileIds(assetId, out var fileIds);

        if (fileIds.Length == 0 || !_store.TryGet(assetId, out _)) return [];

        var result = new AssetFileSpec[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            _store.TryGetFileEntry(fileIds[i], out result[i]);

        return result;
    }

    public override ReadOnlySpan<AssetFileSpec> GetFileSpecs(AssetKind kind) => _store.GetAssetList(kind).GetFileSpan();
}