using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Diagnostics;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class AssetApiController(ApiContext context) : IEngineAssetController
{

    public List<EditorAssetResource> LoadAssetList()
    {
        var store = context.AssetStore;
        var result = new List<EditorAssetResource>(store.Count);
        
        foreach (var obj in store.GetAssetList<Shader>().Asset)
            result.Add(MakeAssetObjectModel(obj));
        foreach (var obj in store.GetAssetList<Model>().Asset)
            result.Add(MakeAssetObjectModel(obj));
        foreach (var obj in store.GetAssetList<Texture2D>().Asset)
            result.Add(MakeAssetObjectModel(obj));
        foreach (var obj in store.GetAssetList<CubeMap>().Asset)
            result.Add(MakeAssetObjectModel(obj));
        foreach (var obj in store.GetAssetList<MaterialTemplate>().Asset)
            result.Add(MakeAssetObjectModel(obj));


        Logger.LogString(LogScope.Engine, $"Editor asset loaded - {result.Count}");
        return result;
    }

    public EditorFileAssetModel[] GetAssetFiles(EditorId editorId)
    {
        var assetTypedId = new AssetId(editorId);
        var store = context.AssetStore;
        store.TryGetFileIds(assetTypedId, out var fileIds);

        if (!store.TryGetByAssetId(assetTypedId, out var asset))
            return [];

        var result = new EditorFileAssetModel[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
        {
            var fileId = fileIds[i];
            store.TryGetFileEntry(fileId, out var entry);
            result[i] = MakeAssetObjectFile(entry!);
        }

        return result;
    }

    public List<EditorAnimationResource> GetAnimationResources()
    {
        var span = context.World.AnimationTableImpl.ModelIdSpan;
        List<EditorAnimationResource> list = new(span.Length);
        context.AssetStore.ExtractList<Model, EditorAnimationResource>(list, static (it) =>
        {
            if (it.AnimationId <= 0) return null!;
            var span = it.Animation!.ClipDataSpan;
            var clips = new EditorAnimationClip[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                clips[i] = new EditorAnimationClip
                {
                    DisplayName = c.Name,
                    Duration = c.Duration,
                    TicksPerSecond = (float)c.TicksPerSecond,
                    TrackCount = c.Tracks.Count
                };
            }

            return new EditorAnimationResource
            {
                Name = it.Name,
                Id = new EditorId(it.AnimationId, EditorItemType.Animation),
                ModelId = new EditorId(it.ModelId, EditorItemType.Model),
                Clips = clips,
                Generation = 1
            };
        });


        return list;
    }

    public static EditorFileAssetModel MakeAssetObjectFile(AssetFileSpec spec) =>
        new()
        {
            AssetFileId = spec.Id.Value,
            RelativePath = spec.RelativePath,
            SizeInBytes = spec.SizeBytes,
            ContentHash = spec.ContentHash
        };

    public static EditorAssetResource MakeAssetObjectModel(AssetObject obj)
    {
        var resourceId = 0;
        string resourceName = "", specialName = "", specialValue = "";
        var hasActions = false;

        switch (obj)
        {
            case Shader shader:
                specialName = "Samplers";
                specialValue = shader.Samplers.ToString();
                resourceId = shader.ResourceId;
                hasActions = true;
                resourceName = "GfxId";
                break;
            case Texture2D tex:
                specialName = "Size";
                specialValue = $"{tex.Width}X{tex.Height}";
                resourceId = tex.ResourceId;
                resourceName = "TexId";
                break;
            case Model model:
                specialName = "Meshes";
                specialValue = model.MeshParts.Length.ToString();

                resourceId = model.ModelId;
                resourceName = "ModelId";
                break;
            case MaterialTemplate material:
                specialName = "Slots";
                specialValue = material.TextureSlots.AssetSlots.Length.ToString();

                resourceId = material.ShaderRef;
                resourceName = "ShaderRef";
                break;
        }

        return new EditorAssetResource
        {
            Id = new EditorId(obj.Id.Value, obj.Kind.ToEditorEnum()),
            EngineGid = obj.GId,
            Name = obj.Name,
            Kind = obj.Kind,
            ResourceId = resourceId,
            ResourceName = resourceName,
            SpecialName = specialName,
            SpecialValue = specialValue,
            HasActions = hasActions,
            IsCoreAsset = obj.IsCoreAsset,
            Generation = obj.Generation,
        };
    }
}