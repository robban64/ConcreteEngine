using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Worlds;
using Core.DebugTools.Data;

namespace ConcreteEngine.Core.Diagnostic;

internal static class EditorRouter
{
    private static World? _world;
    private static AssetSystem? _assetSystem;
    
    
    internal static void Attach(World world, AssetSystem assetSystem)
    {
        _world = world;
        _assetSystem = assetSystem;
    }
    
    public static List<AssetObjectFileViewModel> FetchAssetObjectFiles (AssetObjectViewModel asset)
    {
        if (_assetSystem is null) return [];
        var store = _assetSystem.StoreImpl;
        var result = new List<AssetObjectFileViewModel>(2);
        store.TryGetFileIds(new AssetId(asset.AssetId), out var fileIds);
        foreach (var fileId in fileIds)
        {
            store.TryGetFileEntry(fileId, out var entry);
            result.Add(MakeAssetObjectFile(entry!));
        }
        return result;
    }

    public static void DrainAssetStoreData(AssetStoreViewModel viewModel)
    {
        if (_assetSystem is null) return;
        var store = _assetSystem.StoreImpl;
        var meta = store.GetMetaSnapshot<Shader>();

        var resultList = viewModel.AssetObjects;
        if (meta.Count != resultList.Count)
        {
            resultList.Clear();
            foreach (var obj in store.AssetValues)
            {
                if (obj is not Shader it) continue;
                resultList.Add(MakeAssetObjectModel(it));
            }

            resultList.Sort(static (a, b) => a.AssetId.CompareTo(b.AssetId));

            return;
        }

        int idx = 0;
        foreach (var obj in store.AssetValues)
        {
            if (obj is not Shader it) continue;
            var model = resultList[idx];
            if (it.RawId == model.AssetId && it.Generation != model.Generation)
                resultList[idx] = MakeAssetObjectModel(it);
        }

        viewModel.AssetKind = "Shader";
    }

    private static AssetObjectViewModel MakeAssetObjectModel(Shader shader) =>
        new(shader.RawId, shader.ResourceId, shader.Name, shader.IsCoreAsset, shader.Generation,
            shader.Kind.ToLogTopic().ToLogText());
    
    private static AssetObjectFileViewModel MakeAssetObjectFile(AssetFileEntry entry) =>
        new(entry.Id.Value,entry.RelativePath,entry.SizeBytes,entry.ContentHash);

}