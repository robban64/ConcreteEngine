using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Worlds.Entities;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Core.Editor;

internal static class EditorObjectMapper
{
    public static EntityViewModel MakeEntityViewModel(EntityId id, in ModelComponent model, in Transform transform)
    {
        var transformData =
            new EditorTransform(in transform.Translation, in transform.Scale, in transform.Rotation);

        var modelData = new EntityEditorModel(model.Model, model.MaterialKey.Value, model.DrawCount);

        return new EntityViewModel(
            entityId: id,
            name: string.Empty,
            componentCount: 2,
            model: in modelData,
            transform: in transformData);
    }

    public static AssetObjectFileViewModel MakeAssetObjectFile(AssetFileEntry entry) =>
        new(entry.Id.Value, entry.RelativePath, entry.SizeBytes, entry.ContentHash);

    public static AssetObjectViewModel MakeAssetObjectModel(AssetObject obj)
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

                resourceId = material.ShaderRef.Value;
                resourceName = "ShaderRef";
                break;
        }


        return new AssetObjectViewModel(obj.RawId,
            resourceId,
            resourceName,
            obj.Name,
            obj.IsCoreAsset,
            obj.Generation,
            specialName,
            specialValue,
            hasActions);
    }
}