using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;

namespace ConcreteEngine.Engine.Editor;

internal static class EditorObjectMapper
{
    public static EditorFileAssetModel MakeAssetObjectFile(AssetFileEntry entry) =>
        new()
        {
            AssetFileId = entry.Id.Value,
            RelativePath = entry.RelativePath,
            SizeInBytes = entry.SizeBytes,
            ContentHash = entry.ContentHash
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
            Id = new EditorId(obj.RawId.Value, obj.Kind.ToEditorEnum()),
            Name = obj.Name,
            AssetCategory = obj.Kind.ToEditorCategory(),
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