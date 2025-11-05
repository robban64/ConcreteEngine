using System.Text;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Core.Worlds.Entities;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Utils;

namespace ConcreteEngine.Core.Diagnostic.Routers;

internal static class EditorRouter
{
    private static World? _world;
    private static AssetSystem? _assetSystem;


    internal static void Attach(World world, AssetSystem assetSystem)
    {
        _world = world;
        _assetSystem = assetSystem;
    }

    public static void PullEntityView(EntityListViewModel viewModel)
    {
        if (_world is null) return;

        Transform defaultTransform = default;

        foreach (var it in _world.Query<ModelComponent>())
        {
            ref var model = ref it.Component;

            ref var transform = ref _world.Transforms.Has(it.Entity)
                ? ref _world.Transforms.GetById(it.Entity)
                : ref defaultTransform;

            viewModel.Entities.Add(EditorObjectMapper.MakeEntityViewModel(it.Entity, in model, in transform));
        }
    }

    public static void PullAssetObjectFiles(AssetObjectViewModel asset, List<AssetObjectFileViewModel> result)
    {
        if (_assetSystem is null) return;
        var store = _assetSystem.StoreImpl;
        store.TryGetFileIds(new AssetId(asset.AssetId), out var fileIds);
        foreach (var fileId in fileIds)
        {
            store.TryGetFileEntry(fileId, out var entry);
            result.Add(EditorObjectMapper.MakeAssetObjectFile(entry!));
        }
    }

    public static void PullAssetStoreData(EditorAssetSelection selection, List<AssetObjectViewModel> result)
    {
        if (_assetSystem is null) return;
        if (selection == EditorAssetSelection.None) return;
        var store = _assetSystem.StoreImpl;
        var type = EditorEnumMapper.AssetSelectionToType(selection);

        foreach (var obj in store.AssetValues)
        {
            if (obj.GetType() != type) continue;
            result.Add(EditorObjectMapper.MakeAssetObjectModel(obj));
        }

        result.Sort(static (a, b) => a.AssetId.CompareTo(b.AssetId));
    }
}