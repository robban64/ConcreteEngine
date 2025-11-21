#region

using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EditorEnumMapper
{
    public static Type AssetSelectionToType(EditorAssetCategory category)
    {
        return category switch
        {
            EditorAssetCategory.Shader => typeof(Shader),
            EditorAssetCategory.Texture => typeof(Texture2D),
            EditorAssetCategory.Model => typeof(Model),
            EditorAssetCategory.Material => typeof(MaterialTemplate),
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }
}