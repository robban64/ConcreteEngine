#region

using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Meshes;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;

#endregion

namespace ConcreteEngine.Engine.Editor;

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