using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Core.Editor;

internal static class EditorEnumMapper
{
    public static Type AssetSelectionToType(EditorAssetSelection selection)
    {
        return selection switch
        {
            EditorAssetSelection.Shader => typeof(Shader),
            EditorAssetSelection.Texture => typeof(Texture2D),
            EditorAssetSelection.Model => typeof(Model),
            EditorAssetSelection.Material => typeof(MaterialTemplate),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
        };
    }
}