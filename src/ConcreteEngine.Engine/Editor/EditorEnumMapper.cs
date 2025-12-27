using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Editor;

internal static class EditorEnumMapper
{
    extension(EditorItemType itemType)
    {
        public Type ToAssetType()
        {
            return itemType switch
            {
                EditorItemType.Texture => typeof(Texture2D),
                EditorItemType.Shader => typeof(Shader),
                EditorItemType.Model => typeof(Model),
                EditorItemType.MaterialTemplate => typeof(MaterialTemplate),
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }
    }

    extension(AssetKind itemType)
    {
        public EditorItemType ToEditorEnum()
        {
            return itemType switch
            {
                AssetKind.Texture2D => EditorItemType.Texture,
                AssetKind.TextureCubeMap => EditorItemType.Texture,
                AssetKind.Shader => EditorItemType.Shader,
                AssetKind.Model => EditorItemType.Model,
                AssetKind.MaterialTemplate => EditorItemType.MaterialTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }
    }
}