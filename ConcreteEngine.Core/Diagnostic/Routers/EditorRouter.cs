using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Worlds;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;

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

    public static void FetchAssetObjectFiles(AssetObjectViewModel asset, List<AssetObjectFileViewModel> result)
    {
        if (_assetSystem is null) return;
        var store = _assetSystem.StoreImpl;
        store.TryGetFileIds(new AssetId(asset.AssetId), out var fileIds);
        foreach (var fileId in fileIds)
        {
            store.TryGetFileEntry(fileId, out var entry);
            result.Add(MakeAssetObjectFile(entry!));
        }
    }

    public static void DrainAssetStoreData(EditorAssetSelection selection, List<AssetObjectViewModel> result)
    {
        if (_assetSystem is null) return;
        if (selection == EditorAssetSelection.None) return;
        var store = _assetSystem.StoreImpl;
        var type = selection switch
        {
            EditorAssetSelection.Shader => typeof(Shader),
            EditorAssetSelection.Texture => typeof(Texture2D),
            EditorAssetSelection.Model => typeof(Model),
            EditorAssetSelection.Material => typeof(MaterialTemplate),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
        };

        foreach (var obj in store.AssetValues)
        {
            if (obj.GetType() != type) continue;
            result.Add(MakeAssetObjectModel(obj));
        }

        result.Sort(static (a, b) => a.AssetId.CompareTo(b.AssetId));
    }

    private static AssetObjectViewModel MakeAssetObjectModel(AssetObject obj)
    {
        int resourceId = 0;
        string resourceName = "GfxId";

        string specialName = "";
        string specialValue = "";

        if (obj is Shader shader)
        {
            specialName = "Samplers";
            specialValue = shader.Samplers.ToString();
            resourceId = shader.ResourceId;
        }
        else if (obj is Texture2D tex)
        {
            specialName = "Size";
            specialValue = $"{tex.Width}X{tex.Height}";
            resourceId = tex.ResourceId;
        }
        else if (obj is Model model)
        {
            specialName = "Meshes";
            specialValue = model.MeshParts.Length.ToString();

            resourceId = model.ModelId;
            resourceName = "ModelId";
        }
        else if (obj is MaterialTemplate material)
        {
            specialName = "Slots";
            specialValue = material.TextureSlots.AssetSlots.Length.ToString();

            resourceId = material.ShaderRef.Value;
            resourceName = "ShaderRef";
        }


        return new AssetObjectViewModel(obj.RawId,
            resourceId,
            resourceName,
            obj.Name,
            obj.IsCoreAsset,
            obj.Generation,
            specialName,
            specialValue);
    }

    private static AssetObjectFileViewModel MakeAssetObjectFile(AssetFileEntry entry) =>
        new(entry.Id.Value, entry.RelativePath, entry.SizeBytes, entry.ContentHash);
}