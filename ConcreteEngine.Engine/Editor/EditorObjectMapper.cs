#region

using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EditorObjectMapper
{

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